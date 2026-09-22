#!/usr/bin/env python3
"""
BuildSmart Self-Healing Bug-Fixing Agent
Analyzes production exceptions (from Sentry/Infra Bot), generates patches and unit tests using Gemini,
verifies that 'dotnet test' passes 100%, and outputs PR metadata for GitHub Actions.
"""

import os
import sys
import json
import re
import subprocess
import urllib.request
import urllib.error

def call_gemini(api_key: str, system_prompt: str, user_prompt: str) -> str:
    url = f"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={api_key}"
    payload = {
        "contents": [
            {
                "role": "user",
                "parts": [{"text": f"{system_prompt}\n\nTask:\n{user_prompt}"}]
            }
        ],
        "generationConfig": {
            "temperature": 0.2,
            "maxOutputTokens": 8192
        }
    }

    req = urllib.request.Request(
        url,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"}
    )

    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            data = json.loads(resp.read().decode("utf-8"))
            candidates = data.get("candidates", [])
            if candidates:
                parts = candidates[0].get("content", {}).get("parts", [])
                if parts:
                    return parts[0].get("text", "")
    except Exception as e:
        print(f"[SelfHeal] Gemini API call failed: {e}", file=sys.stderr)
        return ""
    return ""

def main():
    api_key = os.environ.get("GEMINI_API_KEY")
    if not api_key:
        print("[SelfHeal] Error: GEMINI_API_KEY environment variable is missing.", file=sys.stderr)
        sys.exit(1)

    error_title = os.environ.get("ERROR_TITLE", "Application Exception")
    error_message = os.environ.get("ERROR_MESSAGE", "Unhandled exception occurred")
    stack_trace = os.environ.get("STACK_TRACE", "")
    file_path = os.environ.get("FILE_PATH", "")
    line_number = os.environ.get("LINE_NUMBER", "")

    print(f"[SelfHeal] Starting investigation for: {error_title}")
    print(f"[SelfHeal] File: {file_path}:{line_number}")

    # Resolve target file if path is relative or approximate
    resolved_path = None
    if file_path and os.path.exists(file_path):
        resolved_path = file_path
    elif file_path:
        base_name = os.path.basename(file_path)
        for root, _, files in os.walk("."):
            if base_name in files:
                resolved_path = os.path.join(root, base_name)
                break

    if not resolved_path:
        print(f"[SelfHeal] Could not locate file {file_path} in repository.", file=sys.stderr)
        sys.exit(1)

    with open(resolved_path, "r", encoding="utf-8") as f:
        file_content = f.read()

    system_prompt = """
You are a Principal .NET 9 / C# Software Engineer.
Your mission is to autonomously fix a production bug in the BuildSmart codebase.
Rules:
1. Preserve all existing architecture, namespaces, and conventions.
2. Do not introduce breaking changes or modify unrelated code.
3. Fix the root cause cleanly (e.g. null checks, bounds checking, fallback logic).
4. Provide the COMPLETE updated C# code for the affected file inside a ```csharp:file block.
5. Provide a standalone xUnit unit test inside a ```csharp:test block that verifies the fix.
6. Provide a concise Pull Request description in markdown.
"""

    user_prompt = f"""
Bug Report:
- Error: {error_title}
- Message: {error_message}
- Line: {line_number}
- Stack Trace:
{stack_trace}

Target File ({resolved_path}):
```csharp
{file_content}
```

Please provide:
1. The fixed C# code for {resolved_path} in a ```csharp:file ... ``` block.
2. A new unit test in a ```csharp:test ... ``` block.
3. A brief explanation for the PR.
"""

    response = call_gemini(api_key, system_prompt, user_prompt)
    if not response:
        print("[SelfHeal] Failed to receive fix from Gemini.", file=sys.stderr)
        sys.exit(1)

    # Extract fixed file
    file_match = re.search(r"```csharp:file\s*(.*?)\s*```", response, re.DOTALL)
    if not file_match:
        file_match = re.search(r"```csharp\s*(.*?)\s*```", response, re.DOTALL)

    if not file_match:
        print("[SelfHeal] Could not extract fixed code block from AI response.", file=sys.stderr)
        sys.exit(1)

    fixed_code = file_match.group(1)

    # Backup original file
    backup_path = resolved_path + ".bak"
    with open(backup_path, "w", encoding="utf-8") as f:
        f.write(file_content)

    # Write patched file
    with open(resolved_path, "w", encoding="utf-8") as f:
        f.write(fixed_code)

    # Extract unit test if present
    test_match = re.search(r"```csharp:test\s*(.*?)\s*```", response, re.DOTALL)
    test_path = None
    if test_match:
        test_code = test_match.group(1)
        test_path = os.path.join("BuildSmart.Api.Tests", f"AutoFix_{os.path.basename(resolved_path).replace('.cs', '')}Tests.cs")
        with open(test_path, "w", encoding="utf-8") as f:
            f.write(test_code)
        print(f"[SelfHeal] Generated verification test: {test_path}")

    # Verify with 'dotnet test'
    print("[SelfHeal] Verifying fix with dotnet test...")
    test_proc = subprocess.run(
        ["dotnet", "test", "BuildSmart.Api.Tests/BuildSmart.Api.Tests.csproj", "-c", "Release", "--verbosity", "minimal"],
        capture_output=True,
        text=True
    )

    if test_proc.returncode != 0:
        print(f"[SelfHeal] Tests failed after applying fix:\n{test_proc.stdout}\n{test_proc.stderr}", file=sys.stderr)
        # Revert changes
        with open(backup_path, "r", encoding="utf-8") as f:
            revert_content = f.read()
        with open(resolved_path, "w", encoding="utf-8") as f:
            f.write(revert_content)
        if test_path and os.path.exists(test_path):
            os.remove(test_path)
        if os.path.exists(backup_path):
            os.remove(backup_path)
        sys.exit(1)

    if os.path.exists(backup_path):
        os.remove(backup_path)

    print("[SelfHeal] SUCCESS: All unit tests passed cleanly!")

    # Write PR description artifact
    pr_body = f"""## 🛠️ Autonomous Self-Healing Bug Fix

### Problem
- **Issue:** `{error_title}`
- **Message:** `{error_message}`
- **Location:** `{resolved_path}:{line_number}`

### Solution
- Applied targeted patch in `{resolved_path}`.
- Added regression unit tests to verify the fix.
- Automated validation: `dotnet test` passed 100% with 0 errors.

---
*Generated autonomously by BuildSmart Infra Bot & Self-Healing Agent.*
"""

    with open("pr_description.md", "w", encoding="utf-8") as f:
        f.write(pr_body)

    sys.exit(0)

if __name__ == "__main__":
    main()
