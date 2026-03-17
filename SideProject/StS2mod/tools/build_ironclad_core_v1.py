import json
import re
from copy import deepcopy
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CORE_PATH = ROOT / "data" / "cards.core.json"
LEGACY_CARDS_PATH = ROOT / "Astrolabe" / "data" / "cards.json"
POOL_PATH = ROOT / "decompiled" / "IroncladCardPool.txt"
CARDS_DIR = ROOT / "decompiled" / "cards"


CAMEL_BOUNDARY_RE = re.compile(r"(?<=[a-z0-9])(?=[A-Z])")
WHITESPACE_RE = re.compile(r"\s+")
SPECIAL_CHAR_RE = re.compile(r"[^A-Z0-9_]")
POOL_CARD_RE = re.compile(r"ModelDb\.Card<([A-Za-z0-9_]+)>\(\)")
CLASS_BLOCK_TEMPLATE = r"(?ms)^(?:\t)?public (?:sealed )?class {class_name} : CardModel.*?(?=^(?:\t)?public (?:sealed )?class |\Z)"
BASE_RE = re.compile(r": base\(([-\d]+), CardType\.(\w+), CardRarity\.(\w+), TargetType\.(\w+)\)")
KEYWORD_RE = re.compile(r"CardKeyword\.(Exhaust|Ethereal|Retain|Innate)")
DAMAGE_VAR_RE = re.compile(r"new DamageVar\(([-\d.]+)m")
BLOCK_VAR_RE = re.compile(r"new BlockVar\(([-\d.]+)m")
DRAW_VAR_RE = re.compile(r"new CardsVar\(([-\d.]+)\)")
ENERGY_VAR_RE = re.compile(r"new EnergyVar\(([-\d.]+)\)")
HP_LOSS_VAR_RE = re.compile(r"new HpLossVar\(([-\d.]+)m\)")
POWER_VAR_RE = re.compile(r"new PowerVar<([A-Za-z0-9_]+)>\(([-\d.]+)m\)")
NAMED_VAR_RE = re.compile(r"new DynamicVar\(\"([A-Za-z0-9_]+)\",\s*([-\d.]+)m\)")
HIT_COUNT_RE = re.compile(r"\.WithHitCount\((\d+)\)")
UPGRADE_DYNAMIC_RE = re.compile(r"base\.DynamicVars(?:\.(\w+)|\[\"([^\"]+)\"\])\.UpgradeValueBy\(([-\d.]+)m\)")
APPLY_CALL_RE = re.compile(
    r"PowerCmd\.Apply<([A-Za-z0-9_]+)>\((.*?)\)",
    re.S,
)
BASE_VALUE_ACCESSOR_RE = re.compile(r"base\.DynamicVars(?:\.(\w+)|\[\"([^\"]+)\"\])\.BaseValue")
CREATE_CLONE_RE = re.compile(r"CardModel\s+(\w+)\s*=\s*CreateClone\(\)")
CREATE_CARD_RE = re.compile(r"CardModel\s+(\w+)\s*=\s*base\.CombatState\.CreateCard<([A-Za-z0-9_]+)>\(")
ADD_GENERATED_RE = re.compile(r"AddGeneratedCardToCombat\((\w+),\s*PileType\.(\w+)")
UPGRADES_HAND_RE = re.compile(r"CardSelectCmd\.FromHandForUpgrade\(|PileType\.Hand\.GetPile\(base\.Owner\).*?CardCmd\.Upgrade\(", re.S)
UPGRADES_DECK_RE = re.compile(r"PlayerCombatState\.AllCards.*?CardCmd\.Upgrade\(", re.S)
EXHAUST_ONE_RE = re.compile(r"CardCmd\.Exhaust\(")
SCALE_BLOCK_RE = re.compile(r"Owner\.Creature\.Block")
SCALE_EXHAUST_RE = re.compile(r"PileType\.Exhaust\.GetPile")
SCALE_STRIKE_RE = re.compile(r"CardTag\.Strike|Strike", re.S)

STATUS_CARD_IDS = {
    "BURN",
    "DAZED",
    "SLIMED",
    "WOUND",
    "VOID",
    "ASH",
}
SCALING_APPLY_KEYS = {
    "strength",
    "dexterity",
    "metallicize",
    "ritual",
    "plated_armor",
    "barricade",
    "combust",
    "inferno",
    "rupture",
    "juggernaut",
    "dark_embrace",
    "feel_no_pain",
    "corruption",
    "demon_form",
}


def slugify(value: str) -> str:
    text = CAMEL_BOUNDARY_RE.sub("_", value.strip())
    text = WHITESPACE_RE.sub("_", text)
    return SPECIAL_CHAR_RE.sub("", text.upper())


def compact_id(value: str) -> str:
    return slugify(value).replace("_", "").replace("+", "")


def to_int(raw: str) -> int:

    return int(float(raw))


def default_profile() -> dict:
    return {
        "damage": None,
        "block": None,
        "draw": None,
        "energy_gain": None,
        "hits": None,
        "discard": None,
        "exhaust_count": None,
        "hp_cost": None,
        "apply": {},
        "consume": {},
        "create_cards": [],
        "add_status": {},
        "scale_from": [],
    }


def default_flags() -> dict:
    return {
        "exhaust": False,
        "ethereal": False,
        "retain": False,
        "innate": False,
        "x_cost": False,
        "self_replicate": False,
        "upgrades_hand": False,
        "upgrades_deck": False,
    }


def unique(items: list[str]) -> list[str]:
    result: list[str] = []
    seen: set[str] = set()
    for item in items:
        if not item or item in seen:
            continue
        seen.add(item)
        result.append(item)
    return result


def isolate_class_block(file_text: str, class_name: str) -> str:
    start_pattern = re.compile(rf"(?m)^(?:\t)?public (?:sealed )?class {re.escape(class_name)} : CardModel")
    start_match = start_pattern.search(file_text)
    if not start_match:
        raise ValueError(f"Could not isolate class block for {class_name}")

    brace_start = file_text.find("{", start_match.end())
    if brace_start < 0:
        raise ValueError(f"Could not find class body for {class_name}")

    depth = 0
    for index in range(brace_start, len(file_text)):
        char = file_text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return file_text[start_match.start():index + 1]

    raise ValueError(f"Could not find class end for {class_name}")



def extract_method_block(class_block: str, method_name: str) -> str:
    markers = [
        f"protected override async Task {method_name}",
        f"protected override Task {method_name}",
        f"protected override void {method_name}",
    ]

    start = -1
    for marker in markers:
        start = class_block.find(marker)
        if start >= 0:
            break

    if start < 0:
        return ""


    brace_start = class_block.find("{", start)
    if brace_start < 0:
        return ""

    depth = 0
    for index in range(brace_start, len(class_block)):
        char = class_block[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return class_block[brace_start:index + 1]
    return ""


def normalize_apply_key(power_type: str) -> str:
    base = power_type[:-5] if power_type.endswith("Power") else power_type
    return slugify(base).lower()


def build_var_aliases(class_block: str) -> dict[str, int]:
    aliases: dict[str, int] = {}

    for power_name, value in POWER_VAR_RE.findall(class_block):
        numeric = to_int(value)
        base_name = power_name[:-5] if power_name.endswith("Power") else power_name
        aliases[slugify(base_name).lower()] = numeric
        aliases[slugify(power_name).lower()] = numeric

    for var_name, value in NAMED_VAR_RE.findall(class_block):
        aliases[slugify(var_name).lower()] = to_int(value)

    if DAMAGE_VAR_RE.search(class_block):
        aliases["damage"] = to_int(DAMAGE_VAR_RE.search(class_block).group(1))
    if BLOCK_VAR_RE.search(class_block):
        aliases["block"] = to_int(BLOCK_VAR_RE.search(class_block).group(1))
    if DRAW_VAR_RE.search(class_block):
        aliases["cards"] = to_int(DRAW_VAR_RE.search(class_block).group(1))
    if ENERGY_VAR_RE.search(class_block):
        aliases["energy"] = to_int(ENERGY_VAR_RE.search(class_block).group(1))
    if HP_LOSS_VAR_RE.search(class_block):
        aliases["hploss"] = to_int(HP_LOSS_VAR_RE.search(class_block).group(1))

    return aliases


def resolve_apply_value(argument_blob: str, aliases: dict[str, int]) -> int | None:
    accessor_match = BASE_VALUE_ACCESSOR_RE.search(argument_blob)
    if accessor_match:
        accessor = accessor_match.group(1) or accessor_match.group(2) or ""
        return aliases.get(slugify(accessor).lower())

    number_match = re.search(r",\s*([-\d.]+)m\s*,", argument_blob)
    if number_match:
        return to_int(number_match.group(1))

    number_match = re.search(r",\s*([-\d.]+)m\s*\)", argument_blob)
    if number_match:
        return to_int(number_match.group(1))

    return None


def apply_upgrade_delta(current: int | None, delta: int) -> int:
    return (current or 0) + delta


def parse_card_fact(class_name: str) -> dict:
    file_path = CARDS_DIR / f"{class_name}.txt"
    file_text = file_path.read_text(encoding="utf-8-sig")

    class_block = isolate_class_block(file_text, class_name)
    on_play_block = extract_method_block(class_block, "OnPlay")
    on_upgrade_block = extract_method_block(class_block, "OnUpgrade")

    base_profile = default_profile()
    upgraded_profile = default_profile()
    flags = default_flags()

    base_match = BASE_RE.search(class_block)
    if not base_match:
        raise ValueError(f"Missing constructor base(...) for {class_name}")

    aliases = build_var_aliases(class_block)

    card_fact = {
        "card_id": slugify(class_name),
        "cost": int(base_match.group(1)),
        "type": base_match.group(2),
        "rarity": base_match.group(3),
        "target": base_match.group(4),
        "effects": {
            "base": base_profile,
            "upgraded": upgraded_profile,
        },
        "flags": flags,
    }

    damage_match = DAMAGE_VAR_RE.search(class_block)
    if damage_match:
        base_profile["damage"] = to_int(damage_match.group(1))

    block_match = BLOCK_VAR_RE.search(class_block)
    if block_match:
        base_profile["block"] = to_int(block_match.group(1))

    draw_match = DRAW_VAR_RE.search(class_block)
    if draw_match:
        base_profile["draw"] = to_int(draw_match.group(1))

    energy_match = ENERGY_VAR_RE.search(class_block)
    if energy_match:
        base_profile["energy_gain"] = to_int(energy_match.group(1))

    hp_loss_match = HP_LOSS_VAR_RE.search(class_block)
    if hp_loss_match:
        base_profile["hp_cost"] = to_int(hp_loss_match.group(1))

    hit_match = HIT_COUNT_RE.search(on_play_block)
    if hit_match:
        base_profile["hits"] = int(hit_match.group(1))

    canonical_keywords_match = re.search(
        r"(?:public|protected) override IEnumerable<CardKeyword> CanonicalKeywords => .*?;",
        class_block,
        re.S,
    )
    canonical_keywords_source = canonical_keywords_match.group(0) if canonical_keywords_match else ""
    for keyword in KEYWORD_RE.findall(canonical_keywords_source):
        flags[keyword.lower()] = True


    if "HasEnergyCostX => true" in class_block:
        flags["x_cost"] = True

    if "CreateClone()" in on_play_block:
        flags["self_replicate"] = True

    if UPGRADES_HAND_RE.search(on_play_block):
        flags["upgrades_hand"] = True

    if UPGRADES_DECK_RE.search(on_play_block):
        flags["upgrades_deck"] = True

    if EXHAUST_ONE_RE.search(on_play_block):
        base_profile["exhaust_count"] = max(base_profile["exhaust_count"] or 0, 1)

    if SCALE_BLOCK_RE.search(class_block):
        base_profile["scale_from"].append("block")

    if SCALE_EXHAUST_RE.search(class_block):
        base_profile["scale_from"].append("exhaust_pile")

    if class_name == "PerfectedStrike":
        base_profile["scale_from"].append("strike_cards")
    elif class_name == "BodySlam":
        base_profile["scale_from"].append("block")
    elif class_name == "AshenStrike":
        base_profile["scale_from"].append("exhaust_pile")

    created_vars: dict[str, str] = {}
    for var_name in CREATE_CLONE_RE.findall(on_play_block):
        created_vars[var_name] = card_fact["card_id"]
    for var_name, created_class in CREATE_CARD_RE.findall(on_play_block):
        created_vars[var_name] = slugify(created_class)

    for power_name, argument_blob in APPLY_CALL_RE.findall(on_play_block):
        value = resolve_apply_value(argument_blob, aliases)
        if value is None:
            continue
        base_profile["apply"][normalize_apply_key(power_name)] = value

    for var_name, _pile_type in ADD_GENERATED_RE.findall(on_play_block):
        created_card_id = created_vars.get(var_name)
        if not created_card_id:
            continue
        base_profile["create_cards"].append(created_card_id)
        if created_card_id in STATUS_CARD_IDS:
            base_profile["add_status"][created_card_id.lower()] = base_profile["add_status"].get(created_card_id.lower(), 0) + 1
        if created_card_id == card_fact["card_id"]:
            flags["self_replicate"] = True

    upgraded_profile.update(deepcopy(base_profile))
    upgraded_profile["apply"] = deepcopy(base_profile["apply"])
    upgraded_profile["consume"] = deepcopy(base_profile["consume"])
    upgraded_profile["create_cards"] = list(base_profile["create_cards"])
    upgraded_profile["add_status"] = deepcopy(base_profile["add_status"])
    upgraded_profile["scale_from"] = list(base_profile["scale_from"])

    for accessor_a, accessor_b, raw_delta in UPGRADE_DYNAMIC_RE.findall(on_upgrade_block):
        accessor = accessor_a or accessor_b
        delta = to_int(raw_delta)
        normalized = slugify(accessor).lower()
        if normalized == "damage":
            upgraded_profile["damage"] = apply_upgrade_delta(base_profile["damage"], delta)
        elif normalized == "block":
            upgraded_profile["block"] = apply_upgrade_delta(base_profile["block"], delta)
        elif normalized == "cards":
            upgraded_profile["draw"] = apply_upgrade_delta(base_profile["draw"], delta)
        elif normalized == "energy":
            upgraded_profile["energy_gain"] = apply_upgrade_delta(base_profile["energy_gain"], delta)
        elif normalized == "hploss":
            upgraded_profile["hp_cost"] = apply_upgrade_delta(base_profile["hp_cost"], delta)
        else:
            for apply_key in list(base_profile["apply"].keys()):
                if normalized in {apply_key, apply_key.replace("_", ""), slugify(apply_key).lower()}:
                    upgraded_profile["apply"][apply_key] = apply_upgrade_delta(base_profile["apply"].get(apply_key), delta)
                    break
            else:
                alias_value = aliases.get(normalized)
                if alias_value is not None:
                    upgraded_profile["apply"][normalized] = alias_value + delta

    base_profile["create_cards"] = unique(base_profile["create_cards"])
    upgraded_profile["create_cards"] = unique(upgraded_profile["create_cards"])
    base_profile["scale_from"] = unique(base_profile["scale_from"])
    upgraded_profile["scale_from"] = unique(upgraded_profile["scale_from"])

    return card_fact


def load_ironclad_classes() -> list[str]:
    pool_text = POOL_PATH.read_text(encoding="utf-8-sig")

    return POOL_CARD_RE.findall(pool_text)


def validate_counts(cards: list[dict]) -> dict[str, int]:
    return {
        "with_target": sum(1 for card in cards if card.get("target")),
        "with_damage": sum(1 for card in cards if card["effects"]["base"].get("damage") is not None),
        "with_block": sum(1 for card in cards if card["effects"]["base"].get("block") is not None),
        "with_draw": sum(1 for card in cards if card["effects"]["base"].get("draw") is not None),
        "with_apply": sum(1 for card in cards if card["effects"]["base"].get("apply")),
        "with_create_cards": sum(1 for card in cards if card["effects"]["base"].get("create_cards")),
        "exhaust_cards": sum(1 for card in cards if card["flags"].get("exhaust")),
        "x_cost_cards": sum(1 for card in cards if card["flags"].get("x_cost")),
        "self_replicate_cards": sum(1 for card in cards if card["flags"].get("self_replicate")),
        "upgrades_hand_cards": sum(1 for card in cards if card["flags"].get("upgrades_hand")),
        "upgrades_deck_cards": sum(1 for card in cards if card["flags"].get("upgrades_deck")),
    }


def build_metadata_lookup(cards: list[dict]) -> dict[str, dict]:
    lookup: dict[str, dict] = {}
    for card in cards:
        key = compact_id(card.get("card_id", ""))
        if not key:
            continue
        lookup[key] = {
            "name_zh": card.get("name_zh", ""),
            "name_en": card.get("name_en", ""),
            "character": card.get("character", "IRONCLAD"),
        }
    return lookup


def load_legacy_lookup() -> dict[str, dict]:
    if not LEGACY_CARDS_PATH.exists():
        return {}

    payload = json.loads(LEGACY_CARDS_PATH.read_text(encoding="utf-8-sig"))
    cards = payload.get("cards", payload)
    return build_metadata_lookup(cards)


def build_card_entry(card_id: str, class_name: str, fact: dict, metadata_lookup: dict[str, dict]) -> dict:
    metadata = metadata_lookup.get(compact_id(card_id), {})
    return {
        "card_id": card_id,
        "name_zh": metadata.get("name_zh", ""),
        "name_en": metadata.get("name_en", class_name),
        "character": "IRONCLAD",
        "cost": fact["cost"],
        "type": fact["type"],
        "rarity": fact["rarity"],
        "target": fact["target"],
        "effects": fact["effects"],
        "flags": fact["flags"],
    }


def main() -> None:
    payload = json.loads(CORE_PATH.read_text(encoding="utf-8-sig"))
    existing_cards: list[dict] = payload["cards"]
    metadata_lookup = build_metadata_lookup(existing_cards)
    metadata_lookup.update(load_legacy_lookup())

    ironclad_classes = load_ironclad_classes()
    rebuilt_cards: list[dict] = []

    for class_name in ironclad_classes:
        fact = parse_card_fact(class_name)
        rebuilt_cards.append(build_card_entry(fact["card_id"], class_name, fact, metadata_lookup))

    payload["character"] = "IRONCLAD"
    payload["note"] = "Ironclad facts v1 extracted from decompiled card sources. Facts are objective; advisor priors remain in cards.advisor.json."
    payload["cards"] = rebuilt_cards
    CORE_PATH.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    print(json.dumps({
        "updated": len(rebuilt_cards),
        "first_ids": [card["card_id"] for card in rebuilt_cards[:10]],
        "coverage": validate_counts(rebuilt_cards),
    }, ensure_ascii=False, indent=2))



if __name__ == "__main__":
    main()
