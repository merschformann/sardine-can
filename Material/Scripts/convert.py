#!/usr/bin/env python

# This script converts old .xinst files into the new .json input format.
# It only supports the basics, thus, only works for basic inputs.

import argparse
import json
import xml.etree.ElementTree as ET


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", "-i", required=True, help="Input .xinst file")
    parser.add_argument("--output", "-o", required=True, help="Output .json file")
    return parser.parse_args()


def convert(input_file: str, output_file: str) -> None:
    tree = ET.parse(input_file)
    root = tree.getroot()

    # Convert the XML nodes to dictionaries
    containers = []
    for container in root.findall(".//Container"):
        container_dict = {
            "id": int(container.get("ID")),
            "length": float(container.find("Cube").get("Length")),
            "width": float(container.find("Cube").get("Width")),
            "height": float(container.find("Cube").get("Height")),
            "maxWeight": float(container.get("MaxWeight") or 0),
        }
        containers.append(container_dict)
    pieces = []
    for piece in root.findall(".//Piece"):
        piece_dict = {
            "id": int(piece.get("ID")),
            "weight": float(piece.get("Weight") or 0),
            "cubes": [
                {
                    "x": float(cube.find("Point").get("X")),
                    "y": float(cube.find("Point").get("Y")),
                    "z": float(cube.find("Point").get("Z")),
                    "length": float(cube.get("Length")),
                    "width": float(cube.get("Width")),
                    "height": float(cube.get("Height")),
                }
                for cube in piece.findall(".//Cube")
            ],
        }
        pieces.append(piece_dict)

    # Combine and write data
    data = {"containers": containers, "pieces": pieces}
    with open(output_file, "w") as f:
        json.dump(data, f, indent=4)


def main() -> None:
    args = parse_args()
    convert(args.input, args.output)


if __name__ == "__main__":
    main()
