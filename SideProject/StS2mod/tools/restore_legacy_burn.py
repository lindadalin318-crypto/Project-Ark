import json
import subprocess
from pathlib import Path

ROOT = Path(r"f:/UnityProjects/Project-Ark/SideProject/StS2mod/data")


def slugify(value: str) -> str:
    output: list[str] = []
    for index, char in enumerate(value.strip()):
        if char.isalnum():
            if index > 0 and char.isupper() and value[index - 1].isalnum() and value[index - 1].islower():
                output.append("_")
            output.append(char.upper())
        elif output and output[-1] != "_":
            output.append("_")
    return "".join(output).strip("_")


def read_head_json(relative_path: str) -> dict:
    text = subprocess.check_output(
        ["git", "--no-pager", "show", f"HEAD:{relative_path}"],
        text=True,
        encoding="utf-8",
    )
    return json.loads(text)


def restore_cards() -> None:
    original = read_head_json("SideProject/StS2mod/data/cards.json")
    current_path = ROOT / "cards.json"
    current = json.loads(current_path.read_text(encoding="utf-8"))

    legacy_map = {}
    for card in original["cards"]:
        burn_score = card.get("path_scores", {}).get("burn_build")
        if burn_score is not None:
            legacy_map[slugify(card["card_id"])] = burn_score

    for card in current["cards"]:
        burn_score = legacy_map.get(card["card_id"])
        if burn_score is not None:
            card.setdefault("legacy_path_scores", {})["burn_build"] = burn_score

    current_path.write_text(json.dumps(current, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def restore_relics() -> None:
    original = read_head_json("SideProject/StS2mod/data/relics.json")
    current_path = ROOT / "relics.json"
    current = json.loads(current_path.read_text(encoding="utf-8"))

    legacy_map = {}
    for relic in original["relics"]:
        burn_score = relic.get("path_scores", {}).get("burn_build")
        if burn_score is not None:
            legacy_map[slugify(relic["relic_id"])] = burn_score

    for relic in current["relics"]:
        burn_score = legacy_map.get(relic["relic_id"])
        if burn_score is not None:
            relic.setdefault("legacy_path_scores", {})["burn_build"] = burn_score

    current_path.write_text(json.dumps(current, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    restore_cards()
    restore_relics()
    print("Restored legacy burn_build scores into legacy_path_scores.")
