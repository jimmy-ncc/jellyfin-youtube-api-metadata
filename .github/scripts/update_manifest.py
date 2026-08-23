"""Appends the just-released version to manifest.json, using build.yaml as the source of truth."""

import datetime
import hashlib
import json
import os
import sys

import yaml


def calculate_md5(filepath):
    md5_hash = hashlib.md5()
    with open(filepath, "rb") as f:
        for byte_block in iter(lambda: f.read(4096), b""):
            md5_hash.update(byte_block)
    return md5_hash.hexdigest()


def main():
    with open("build.yaml", "r") as f:
        build_data = yaml.safe_load(f)

    version = str(build_data.get("version"))
    guid = build_data.get("guid")

    tag_name = os.environ.get("RELEASE_TAG") or f"v{version}"
    if tag_name.lstrip("v") != version:
        print(f"Error: release tag {tag_name} does not match build.yaml version {version}")
        sys.exit(1)

    zips = [f for f in os.listdir(".") if f.endswith(".zip")]
    if not zips:
        print("Error: no build .zip found in the current directory")
        sys.exit(1)
    zip_filename = zips[0]

    checksum = calculate_md5(zip_filename)
    repo_name = os.environ.get("REPO_NAME")
    source_url = f"https://github.com/{repo_name}/releases/download/{tag_name}/{zip_filename}"

    with open("manifest.json", "r") as f:
        manifest = json.load(f)

    entry = next((item for item in manifest if item["guid"] == guid), None)
    if not entry:
        print(f"Error: GUID {guid} not found in manifest.json")
        sys.exit(1)

    new_version = {
        "version": version,
        "changelog": build_data.get("changelog", ""),
        "targetAbi": str(build_data.get("targetAbi", "")),
        "sourceUrl": source_url,
        "checksum": checksum,
        "timestamp": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
    }

    # Drop any existing entry for the same version so re-running the workflow stays idempotent.
    entry["versions"] = [v for v in entry["versions"] if v["version"] != version]
    entry["versions"].insert(0, new_version)

    with open("manifest.json", "w") as f:
        json.dump(manifest, f, indent=4)
        f.write("\n")


if __name__ == "__main__":
    main()
