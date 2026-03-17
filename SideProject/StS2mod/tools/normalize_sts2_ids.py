import argparse
import json
import re
from copy import deepcopy
from pathlib import Path
from typing import Any

LEGACY_PATH_MAP = {
    "IRONCLAD": {
        "strength_build": "ironclad_strength",
        "exhaust_build": "ironclad_infinite",
        "block_build": "ironclad_defense",
    },
    "SILENT": {
        "poison_build": "silent_poison",
        "infinite_build": "silent_infinite",
        "shiv_build": "silent_evade",
        "evade_build": "silent_evade",
        "discard_build": "silent_infinite",
    },
    "DEFECT": {
        "frost_build": "defect_frost",
        "lightning_build": "defect_lightning",
        "mixed_build": "defect_mixed",
        "orb_build": "defect_mixed",
    },
    "WATCHER": {
        "divinity_build": "watcher_divinity",
        "retain_build": "watcher_retain",
        "miracle_build": "watcher_miracle",
    },
}

GLOBAL_LEGACY_PATH_MAP = {
    legacy_key: new_key
    for per_character in LEGACY_PATH_MAP.values()
    for legacy_key, new_key in per_character.items()
}


UPPER_SNAKE_RE = re.compile(r"^[A-Z0-9_]+$")
CAMEL_BOUNDARY_RE = re.compile(r"(?<=[a-z0-9])(?=[A-Z])")
WHITESPACE_RE = re.compile(r"\s+")
SPECIAL_CHAR_RE = re.compile(r"[^A-Z0-9_]")


def slugify(value: str) -> str:
    text = CAMEL_BOUNDARY_RE.sub("_", value.strip())
    text = WHITESPACE_RE.sub("_", text)
    return SPECIAL_CHAR_RE.sub("", text.upper())




def normalize_model_id(value: str) -> str:
    if not value:
        return value
    upgraded = value.endswith("+")
    base = value[:-1] if upgraded else value
    normalized = slugify(base)
    return normalized + "+" if upgraded else normalized


def normalize_character_id(value: str) -> str:
    return slugify(value) if value else value


def normalize_path_scores(path_scores: dict[str, Any], character_id: str, report: list[str], owner: str) -> tuple[dict[str, Any], dict[str, Any]]:
    mapping = dict(GLOBAL_LEGACY_PATH_MAP)
    mapping.update(LEGACY_PATH_MAP.get(character_id, {}))
    normalized: dict[str, Any] = {}
    dropped: dict[str, Any] = {}

    for key, score in path_scores.items():
        if key in mapping:
            normalized[mapping[key]] = score
        elif key in mapping.values() or (character_id and key.startswith(character_id.lower() + "_")):
            normalized[key] = score
        else:
            dropped[key] = score

    if dropped:
        report.append(f"{owner}: preserved unsupported path_scores keys {sorted(dropped.keys())} in legacy_path_scores")

    return normalized, dropped




def infer_character_id(path_scores: dict[str, Any]) -> str:
    keys = set(path_scores.keys())
    if {"strength_build", "exhaust_build", "block_build", "burn_build"} & keys or any(key.startswith("ironclad_") for key in keys):
        return "IRONCLAD"
    if {"poison_build", "infinite_build", "shiv_build", "evade_build", "discard_build"} & keys or any(key.startswith("silent_") for key in keys):
        return "SILENT"
    if {"frost_build", "lightning_build", "mixed_build", "orb_build"} & keys or any(key.startswith("defect_") for key in keys):
        return "DEFECT"
    if {"divinity_build", "retain_build", "miracle_build"} & keys or any(key.startswith("watcher_") for key in keys):
        return "WATCHER"
    return ""


def normalize_cards(cards_payload: Any, report: list[str]) -> Any:
    wrapped = isinstance(cards_payload, dict)
    cards = deepcopy(cards_payload["cards"] if wrapped else cards_payload)

    for card in cards:
        original_path_scores = card.get("path_scores", {})
        inferred_character = infer_character_id(original_path_scores)
        card["character"] = normalize_character_id(card.get("character", "") or inferred_character)
        card["card_id"] = normalize_model_id(card.get("card_id", ""))
        normalized_scores, legacy_scores = normalize_path_scores(original_path_scores, card.get("character", ""), report, f"card {card['card_id']}")
        existing_legacy_scores = card.get("legacy_path_scores", {})
        merged_legacy_scores = {**existing_legacy_scores, **legacy_scores}
        card["path_scores"] = normalized_scores
        if merged_legacy_scores:
            card["legacy_path_scores"] = merged_legacy_scores
        else:
            card.pop("legacy_path_scores", None)


    if wrapped:
        result = deepcopy(cards_payload)
        result["character"] = normalize_character_id(result.get("character", "") or infer_character_id(cards[0].get("path_scores", {}) if cards else {}))
        result["note"] = "card_id uses StS2 ModelId.Entry (UPPER_SNAKE_CASE). Upgraded variants append '+' only at runtime and are not stored as separate base entries."
        result["cards"] = cards
        return result
    return cards




def normalize_relics(relics_payload: Any, report: list[str]) -> Any:
    wrapped = isinstance(relics_payload, dict)
    result = deepcopy(relics_payload) if wrapped else None
    relics = deepcopy(relics_payload["relics"] if wrapped else relics_payload)

    if wrapped and result is not None:
        for key in ["ironclad_starter", "silent_starter", "defect_starter", "watcher_starter"]:
            if key in result:
                result[key] = normalize_model_id(result[key])

        for key, value in list(result.items()):
            if key.endswith("_exclusive") and isinstance(value, list):
                result[key] = [normalize_model_id(v) for v in value]

    for relic in relics:
        relic_character = relic.get("character", "")
        if relic_character and relic_character.upper() != "SHARED":
            relic["character"] = normalize_character_id(relic_character)
        else:
            relic["character"] = "SHARED" if relic_character else relic_character
        relic["relic_id"] = normalize_model_id(relic.get("relic_id", ""))
        normalized_scores, legacy_scores = normalize_path_scores(relic.get("path_scores", {}), relic.get("character", ""), report, f"relic {relic['relic_id']}")
        existing_legacy_scores = relic.get("legacy_path_scores", {})
        merged_legacy_scores = {**existing_legacy_scores, **legacy_scores}
        relic["path_scores"] = normalized_scores
        if merged_legacy_scores:
            relic["legacy_path_scores"] = merged_legacy_scores
        else:
            relic.pop("legacy_path_scores", None)


    if wrapped and result is not None:
        result["note"] = "relic_id uses StS2 ModelId.Entry (UPPER_SNAKE_CASE). character='Shared' means available to all."
        result["relics"] = relics
        return result
    return relics



def normalize_buildpaths(paths_payload: Any, report: list[str]) -> Any:
    paths = deepcopy(paths_payload)

    for path in paths:
        path["character"] = normalize_character_id(path.get("character", ""))
        path["core_cards"] = [normalize_model_id(v) for v in path.get("core_cards", [])]
        path["key_relics"] = [normalize_model_id(v) for v in path.get("key_relics", [])]
        path["campfire_upgrades"] = [normalize_model_id(v) for v in path.get("campfire_upgrades", [])]
        path["good_against_bosses"] = [normalize_model_id(v) for v in path.get("good_against_bosses", [])]
        path["bad_against_bosses"] = [normalize_model_id(v) for v in path.get("bad_against_bosses", [])]

        unsupported = [
            key for key in path.get("core_cards", []) if not UPPER_SNAKE_RE.match(key)
        ]
        if unsupported:
            report.append(f"path {path.get('path_id')}: unsupported core_cards after normalization {unsupported}")

    return paths


def normalize_bosses(bosses_payload: Any) -> Any:
    bosses = deepcopy(bosses_payload)

    for boss in bosses:
        boss["boss_id"] = normalize_model_id(boss.get("boss_id", ""))
        boss["counter_cards"] = [normalize_model_id(v) for v in boss.get("counter_cards", [])]
        boss["dangerous_to_paths"] = [str(v).strip().replace('-', '_').lower() for v in boss.get("dangerous_to_paths", [])]

    return bosses


def analyze_payload(cards_payload: Any, relics_payload: Any, buildpaths_payload: Any, bosses_payload: Any) -> dict[str, Any]:

    cards = cards_payload["cards"] if isinstance(cards_payload, dict) else cards_payload
    relics = relics_payload["relics"] if isinstance(relics_payload, dict) else relics_payload
    path_keys = sorted({key for card in cards for key in card.get("path_scores", {}).keys()} | {key for relic in relics for key in relic.get("path_scores", {}).keys()})
    return {
        "card_count": len(cards),
        "relic_count": len(relics),
        "buildpath_count": len(buildpaths_payload),
        "boss_count": len(bosses_payload),
        "characters": sorted({card.get("character", "") for card in cards}),
        "path_keys": path_keys,
        "buildpath_ids": [path.get("path_id", "") for path in buildpaths_payload],
        "boss_ids": [boss.get("boss_id", "") for boss in bosses_payload],
        "cards_with_legacy_scores": sum(1 for card in cards if card.get("legacy_path_scores")),
        "relics_with_legacy_scores": sum(1 for relic in relics if relic.get("legacy_path_scores")),
    }




def dump_json(path: Path, payload: Any) -> None:
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description="Normalize Astrolabe StS2 IDs and path keys.")
    parser.add_argument("--data-dir", default=r"f:/UnityProjects/Project-Ark/SideProject/StS2mod/data")
    parser.add_argument("--apply", action="store_true", help="Write normalized JSON back to data dir.")
    parser.add_argument("--report", default="", help="Optional path to save analysis report as JSON.")
    args = parser.parse_args()

    data_dir = Path(args.data_dir)
    cards_path = data_dir / "cards.json"
    relics_path = data_dir / "relics.json"
    buildpaths_path = data_dir / "buildpaths.json"
    bosses_path = data_dir / "bosses.json"

    cards_payload = json.loads(cards_path.read_text(encoding="utf-8"))
    relics_payload = json.loads(relics_path.read_text(encoding="utf-8"))
    buildpaths_payload = json.loads(buildpaths_path.read_text(encoding="utf-8"))
    bosses_payload = json.loads(bosses_path.read_text(encoding="utf-8"))

    report_lines: list[str] = []
    normalized_cards = normalize_cards(cards_payload, report_lines)
    normalized_relics = normalize_relics(relics_payload, report_lines)
    normalized_buildpaths = normalize_buildpaths(buildpaths_payload, report_lines)
    normalized_bosses = normalize_bosses(bosses_payload)

    analysis = analyze_payload(normalized_cards, normalized_relics, normalized_buildpaths, normalized_bosses)

    analysis["notes"] = report_lines

    if args.report:
        dump_json(Path(args.report), analysis)

    if args.apply:
        dump_json(cards_path, normalized_cards)
        dump_json(relics_path, normalized_relics)
        dump_json(buildpaths_path, normalized_buildpaths)
        dump_json(bosses_path, normalized_bosses)
        print(f"Applied normalization to {data_dir}")


    print(json.dumps(analysis, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
