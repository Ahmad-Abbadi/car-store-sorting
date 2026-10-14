"""Export a readable compatibility table from the authoring catalog."""

import csv
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
catalog = json.loads((ROOT / "source_catalog.json").read_text(encoding="utf-8"))
builds = {(b["baseCarId"], b["code"]): b for b in catalog["builds"]}
columns = ["baseCarId", "supplierName", "standardBuild", "customBuild", "customDisabledParts",
           "customEnabledParts", "frontBumper", "rearBumper", "pipe", "headlight", "spoiler",
           "wheels", "crossCarParts", "verificationNote"]
with (ROOT / "compatibility.csv").open("w", newline="", encoding="utf-8") as stream:
    writer = csv.DictWriter(stream, fieldnames=columns)
    writer.writeheader()
    for car in catalog["baseCars"]:
        identifier = car["id"]
        std = builds[identifier, "STD"]
        cus = builds[identifier, "CUS"]
        names = {part["name"] for part in car["parts"]}
        supplier = car["sourceName"]
        def option(label):
            matches = sorted(n for n in names if label in n)
            return "; ".join(matches) if matches else "No verified alternative"
        writer.writerow({
            "baseCarId": identifier,
            "supplierName": supplier,
            "standardBuild": "Supported: supplier default" if std["supported"] else "Unsupported",
            "customBuild": "Supported: " + str(len(cus["enableChildren"])) + " verified swaps" if cus["supported"] else "Unsupported",
            "customDisabledParts": "; ".join(cus["disableChildren"]),
            "customEnabledParts": "; ".join(cus["enableChildren"]),
            "frontBumper": option("FrontBumper"),
            "rearBumper": option("RearBumper"),
            "pipe": option("Pipe"),
            "headlight": option("Headlight") if identifier != "C002" else
                         "Supplier has Cool and Simple headlights; both stay as supplied; independent swap unverified",
            "spoiler": option("Spoiler") + "; enabled in STD, disabled in CUS",
            "wheels": ("CUS replaces four mounted wheels with " + cus["wheelPrefabPath"] +
                       " at the original mounts" if cus.get("wheelPrefabPath") else
                       "CUS retains the wider supplier wheels; narrower alternatives rejected"),
            "crossCarParts": "Unsupported for this release: fit not verified",
            "verificationNote": cus["verification"],
        })
print("Wrote 15 verified base-car rows to compatibility.csv")
