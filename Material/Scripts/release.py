import argparse
import glob
import os
import re
import xml.etree.ElementTree as ET

VERSION_FILE = os.path.join(os.path.dirname(__file__), "..", "..", "VERSION.txt")


def parse_args() -> argparse.Namespace:
    """
    Parse command line arguments.
    """
    parser = argparse.ArgumentParser(description="Bump the version of the project.")
    parser.add_argument("--version", required=True, help="The version to bump to.")
    return parser.parse_args()


def check_version(version: str) -> None:
    """
    Check whether a given version is in expected format and actually newer than the
    current version.
    """
    if not re.match(r"^\d+\.\d+\.\d+$", version):
        raise Exception(f"Version {version} is not in the format x.y.z")

    with open(VERSION_FILE, "r") as f:
        current_version = f.read().strip()

    def parse_version(version: str):
        return tuple(map(int, version.split(".")))

    if parse_version(version) <= parse_version(current_version):
        raise Exception(
            f"New version {version} is not newer than current version {current_version}"
        )


def bump_version_csproj(csproj_file: str, version: str) -> None:
    """
    Bump the version of a csproj file.
    """
    try:
        tree = ET.parse(csproj_file)
        root = tree.getroot()

        bumped = False
        for elem in root.iter():
            if elem.tag == "Version":
                elem.text = version
                bumped = True

        if not bumped:
            raise Exception("No version element found")

        tree.write(csproj_file)
    except Exception as e:
        print(f"Error bumping version in {csproj_file}: {e}")


def bump_version_main(version: str) -> None:
    """
    Bump the main version file.
    """
    with open(VERSION_FILE, "w") as f:
        f.write(version)


def main() -> None:
    args = parse_args()
    version = args.version

    check_version(version)

    csproj_files = glob.glob(
        os.path.join(os.path.dirname(__file__), "..", "..", "**", "*.csproj"),
        recursive=True,
    )
    for csproj_file in csproj_files:
        if "Test" in csproj_file:
            continue
        bump_version_csproj(csproj_file, version)
    bump_version_main(version)


if __name__ == "__main__":
    main()
