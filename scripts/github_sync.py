import os
import sys
import json
import base64
import urllib.request
import urllib.error
from pathlib import Path

REPO_OWNER = "srikaran3004"
REPO_NAME = "Trinetra"
BRANCH = "main"

def get_token():
    # 1. Environment variable
    token = os.environ.get("GITHUB_PAT") or os.environ.get("GITHUB_PERSONAL_ACCESS_TOKEN")
    if token:
        return token

    # 2. Check mcp_config.json
    user_home = Path.home()
    mcp_config_path = user_home / ".gemini" / "config" / "mcp_config.json"
    if mcp_config_path.exists():
        try:
            with open(mcp_config_path, "r", encoding="utf-8") as f:
                data = json.load(f)
                env = data.get("mcpServers", {}).get("github-mcp-server", {}).get("env", {})
                token = env.get("GITHUB_PERSONAL_ACCESS_TOKEN")
                if token:
                    return token
        except Exception as e:
            print(f"[Warning] Failed to read token from {mcp_config_path}: {e}")

    # 3. Check .env in workspace root
    env_file = Path(__file__).resolve().parent.parent / ".env"
    if env_file.exists():
        try:
            with open(env_file, "r", encoding="utf-8") as f:
                for line in f:
                    if line.startswith("GITHUB_PAT=") or line.startswith("GITHUB_TOKEN="):
                        return line.split("=", 1)[1].strip().strip('"').strip("'")
        except Exception:
            pass

    raise RuntimeError("GitHub Personal Access Token not found. Set GITHUB_PAT or configure mcp_config.json.")

def github_request(endpoint, method="GET", data=None, token=None):
    url = f"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/{endpoint.lstrip('/')}"
    headers = {
        "Authorization": f"Bearer {token}",
        "Accept": "application/vnd.github.v3+json",
        "User-Agent": "Trinetra-GitHub-Sync"
    }
    payload = json.dumps(data).encode("utf-8") if data is not None else None
    req = urllib.request.Request(url, data=payload, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        err_body = e.read().decode("utf-8")
        raise RuntimeError(f"GitHub API error {e.code} on {method} {url}: {err_body}")

def get_latest_commit_sha(token):
    ref_info = github_request(f"git/ref/heads/{BRANCH}", "GET", token=token)
    return ref_info["object"]["sha"]

def get_tree_sha(commit_sha, token):
    commit_info = github_request(f"git/commits/{commit_sha}", "GET", token=token)
    return commit_info["tree"]["sha"]

def create_blob(content_bytes, token):
    b64_content = base64.b64encode(content_bytes).decode("ascii")
    data = {
        "content": b64_content,
        "encoding": "base64"
    }
    blob_info = github_request("git/blobs", "POST", data=data, token=token)
    return blob_info["sha"]

def push_files(file_map, commit_message, token):
    """
    file_map: dict of {repo_relative_path: local_file_or_bytes}
    """
    print(f"Fetching latest commit on branch '{BRANCH}'...")
    parent_commit_sha = get_latest_commit_sha(token)
    base_tree_sha = get_tree_sha(parent_commit_sha, token)

    tree_items = []
    print(f"Creating blobs for {len(file_map)} file(s)...")
    for repo_path, file_src in file_map.items():
        if isinstance(file_src, (str, Path)):
            with open(file_src, "rb") as f:
                content = f.read()
        elif isinstance(file_src, bytes):
            content = file_src
        else:
            content = str(file_src).encode("utf-8")

        blob_sha = create_blob(content, token)
        tree_items.append({
            "path": repo_path.replace("\\", "/"),
            "mode": "100644",
            "type": "blob",
            "sha": blob_sha
        })
        print(f"  + Prepared: {repo_path}")

    print("Creating new Git tree...")
    new_tree = github_request("git/trees", "POST", data={
        "base_tree": base_tree_sha,
        "tree": tree_items
    }, token=token)

    print("Creating commit...")
    new_commit = github_request("git/commits", "POST", data={
        "message": commit_message,
        "tree": new_tree["sha"],
        "parents": [parent_commit_sha]
    }, token=token)

    print(f"Updating branch '{BRANCH}' reference...")
    github_request(f"git/refs/heads/{BRANCH}", "PATCH", data={
        "sha": new_commit["sha"],
        "force": False
    }, token=token)

    print(f"Successfully pushed commit: {new_commit['sha'][:8]} - {commit_message}")
    return new_commit["sha"]

def sync_workspace(commit_message):
    token = get_token()
    workspace_root = Path(__file__).resolve().parent.parent

    # Files/folders to ignore
    ignore_patterns = {
        ".git", ".vs", ".idea", "bin", "obj", "node_modules",
        "__pycache__", ".venv", "env", "dist", "build",
        ".DS_Store", "Thumbs.db"
    }

    file_map = {}
    for root, dirs, files in os.walk(workspace_root):
        dirs[:] = [d for d in dirs if d not in ignore_patterns and not d.startswith(".agents")]
        for file in files:
            full_path = Path(root) / file
            rel_path = full_path.relative_to(workspace_root).as_posix()
            file_map[rel_path] = full_path

    if not file_map:
        print("No files found to push.")
        return

    push_files(file_map, commit_message, token)

if __name__ == "__main__":
    msg = sys.argv[1] if len(sys.argv) > 1 else "Update from Trinetra AI assistant"
    sync_workspace(msg)
