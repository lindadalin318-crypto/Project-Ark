param(
    [string]$LegacyPath = "f:/UnityProjects/Project-Ark/SideProject/StS2mod/data/cards.json",
    [string]$CorePath = "f:/UnityProjects/Project-Ark/SideProject/StS2mod/data/cards.core.json",
    [string]$AdvisorPath = "f:/UnityProjects/Project-Ark/SideProject/StS2mod/data/cards.advisor.json"
)

Add-Type -AssemblyName System.Web.Extensions
$serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
$serializer.MaxJsonLength = 67108864

$json = [System.IO.File]::ReadAllText($LegacyPath, [System.Text.Encoding]::UTF8)
$root = $serializer.DeserializeObject($json)
$cards = @($root['cards'])

function New-JsonList([object]$items)
{
    $list = New-Object System.Collections.ArrayList
    if ($null -ne $items)
    {
        foreach ($item in @($items))
        {
            if ($null -ne $item)
            {
                [void]$list.Add($item)
            }
        }
    }

    return $list
}

$coreCards = foreach ($card in $cards) {



    [ordered]@{
        card_id = $card['card_id']
        name_zh = $card['name_zh']
        name_en = $(if ($card.ContainsKey('name_en')) { $card['name_en'] } else { '' })
        character = $(if ($card['character']) { $card['character'] } else { $root['character'] })
        cost = $(if ($null -ne $card['cost']) { [int]$card['cost'] } else { 1 })
        type = $(if ($card['type']) { $card['type'] } else { 'Skill' })
        rarity = $(if ($card['rarity']) { $card['rarity'] } else { 'Common' })
        target = $(if ($card.ContainsKey('target')) { $card['target'] } else { $null })
        effects = [ordered]@{
            base = [ordered]@{
                damage = $null
                block = $null
                draw = $null
                energy_gain = $null
                hits = $null
                discard = $null
                exhaust_count = $null
                hp_cost = $null
                apply = [ordered]@{}
                consume = [ordered]@{}
                create_cards = '__EMPTY_ARRAY__'
                add_status = [ordered]@{}
                scale_from = '__EMPTY_ARRAY__'
            }
            upgraded = [ordered]@{
                damage = $null
                block = $null
                draw = $null
                energy_gain = $null
                hits = $null
                discard = $null
                exhaust_count = $null
                hp_cost = $null
                apply = [ordered]@{}
                consume = [ordered]@{}
                create_cards = '__EMPTY_ARRAY__'
                add_status = [ordered]@{}
                scale_from = '__EMPTY_ARRAY__'
            }


        }
        flags = [ordered]@{
            exhaust = $null
            ethereal = $null
            retain = $null
            innate = $null
            x_cost = $(if (($null -ne $card['cost']) -and ([int]$card['cost'] -eq -1)) { $true } else { $null })
            self_replicate = $null
            upgrades_hand = $null
            upgrades_deck = $null
        }
    }
}

$advisorCards = foreach ($card in $cards) {
    [ordered]@{
        card_id = $card['card_id']
        character = $(if ($card['character']) { $card['character'] } else { $root['character'] })
        base_score = $(if ($null -ne $card['base_score']) { [double]$card['base_score'] } else { 0 })
        tier = $(if ($card['tier']) { $card['tier'] } else { 'C' })
        path_scores = $(if ($card['path_scores']) { $card['path_scores'] } else { [ordered]@{} })
        legacy_path_scores = $(if ($card['legacy_path_scores']) { $card['legacy_path_scores'] } else { [ordered]@{} })
        synergy_tags = (New-JsonList $card['synergy_tags'])
        anti_synergy_tags = (New-JsonList $card['anti_synergy_tags'])
        act_scaling = $(if ($card['act_scaling']) { New-JsonList $card['act_scaling'] } else { New-JsonList @(1, 1, 1) })
        upgrade_priority = $(if ($card['upgrade_priority']) { $card['upgrade_priority'] } else { 'medium' })
        upgrade_delta_zh = $(if ($card['upgrade_delta_zh']) { $card['upgrade_delta_zh'] } else { '' })
        notes_zh = $(if ($card['notes_zh']) { $card['notes_zh'] } else { '' })
        advisor = [ordered]@{
            roles = '__EMPTY_ARRAY__'
            duplicate_policy = 'neutral'
            deck_pressure = 'neutral'
            pickup_windows = '__EMPTY_ARRAY__'
            upgrade_spike = $null
            remove_priority = $null
        }


    }
}

$coreRoot = [ordered]@{
    version = '3.0-core'
    source = $root['source']
    character = $root['character']
    note = 'Split from legacy cards.json. Stores card facts and v2.5 structural slots.'
    cards = $coreCards
}

$advisorRoot = [ordered]@{
    version = '3.0-advisor'
    source = $root['source']
    character = $root['character']
    note = 'Split from legacy cards.json. Stores advisor priors and v2.5 guidance fields.'
    cards = $advisorCards
}

$utf8 = New-Object System.Text.UTF8Encoding($false)
$coreJson = ($coreRoot | ConvertTo-Json -Depth 12)
$advisorJson = ($advisorRoot | ConvertTo-Json -Depth 12)
$coreJson = $coreJson.Replace('"create_cards":  "__EMPTY_ARRAY__"', '"create_cards": []')
$coreJson = $coreJson.Replace('"scale_from":  "__EMPTY_ARRAY__"', '"scale_from": []')
$advisorJson = $advisorJson.Replace('"roles":  "__EMPTY_ARRAY__"', '"roles": []')
$advisorJson = $advisorJson.Replace('"pickup_windows":  "__EMPTY_ARRAY__"', '"pickup_windows": []')
$advisorJson = [System.Text.RegularExpressions.Regex]::Replace($advisorJson, '"synergy_tags":\s*\{\s*\}', '"synergy_tags": []')
$advisorJson = [System.Text.RegularExpressions.Regex]::Replace($advisorJson, '"anti_synergy_tags":\s*\{\s*\}', '"anti_synergy_tags": []')
$advisorJson = [System.Text.RegularExpressions.Regex]::Replace($advisorJson, '"synergy_tags":\s*"([^"]*)"', '"synergy_tags": ["$1"]')
$advisorJson = [System.Text.RegularExpressions.Regex]::Replace($advisorJson, '"anti_synergy_tags":\s*"([^"]*)"', '"anti_synergy_tags": ["$1"]')

[System.IO.File]::WriteAllText($CorePath, $coreJson, $utf8)
[System.IO.File]::WriteAllText($AdvisorPath, $advisorJson, $utf8)

Write-Output ("CORE=" + $coreCards.Count)
Write-Output ("ADVISOR=" + $advisorCards.Count)





