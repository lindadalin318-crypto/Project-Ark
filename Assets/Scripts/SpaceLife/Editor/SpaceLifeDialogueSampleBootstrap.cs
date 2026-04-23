using System;
using System.Collections.Generic;
using System.Reflection;
using ProjectArk.SpaceLife.Data;
using ProjectArk.SpaceLife.Dialogue;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectArk.SpaceLife.Editor
{
    /// <summary>
    /// Creates the authored dialogue assets and wires a minimal 1 NPC + 1 terminal sample slice into SampleScene.
    /// </summary>
    public static class SpaceLifeDialogueSampleBootstrap
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        private const string DataRootFolder = "Assets/_Data/SpaceLife";
        private const string DialogueFolder = DataRootFolder + "/Dialogue";
        private const string DialogueGraphsFolder = DialogueFolder + "/Graphs";
        private const string NpcFolder = DataRootFolder + "/NPC";
        private const string ItemFolder = DataRootFolder + "/Items";

        private const string DialogueDatabasePath = DialogueFolder + "/DialogueDatabase.asset";
        private const string EngineerGraphPath = DialogueGraphsFolder + "/NPC_Engineer_HubDialogue.asset";
        private const string TerminalGraphPath = DialogueGraphsFolder + "/Terminal_ShipAI_HubDialogue.asset";
        private const string EngineerNpcDataPath = NpcFolder + "/NPC_Engineer.asset";
        private const string EngineerGiftItemPath = ItemFolder + "/Gift_Engineer_CalibrationKit.asset";

        private const string SpaceLifeSceneRootName = "SpaceLifeScene";
        private const string SampleSliceRootName = "SpaceLifeDialogueSampleSlice";
        private const string CoordinatorObjectName = "SpaceLifeDialogueCoordinator";
        private const string RouterObjectName = "DialogueServiceRouter";
        private const string SampleNpcName = "EngineerDialogueSample";
        private const string SampleTerminalName = "ShipAITerminalSample";
        private const string SampleDebugObjectName = "SpaceLifeDialogueSampleDebug";

        private const string EngineerOwnerId = "engineer_hub";
        private const string TerminalOwnerId = "ship_ai_terminal";
        private const string EngineerFirstMetFlag = "spacelife.engineer.first_met";

        [MenuItem("ProjectArk/Space Life/Dialogue/Bootstrap Hub Sample Slice", priority = 70)]
        public static void BootstrapHubSampleSlice()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureDataFolders();

            ItemSO engineerGift = LoadOrCreateAsset<ItemSO>(EngineerGiftItemPath);
            ConfigureEngineerGift(engineerGift);

            NPCDataSO engineerNpcData = LoadOrCreateAsset<NPCDataSO>(EngineerNpcDataPath);
            ConfigureEngineerNpcData(engineerNpcData, engineerGift);

            DialogueGraphSO engineerGraph = LoadOrCreateAsset<DialogueGraphSO>(EngineerGraphPath);
            ConfigureEngineerGraph(engineerGraph);

            DialogueGraphSO terminalGraph = LoadOrCreateAsset<DialogueGraphSO>(TerminalGraphPath);
            ConfigureTerminalGraph(terminalGraph);

            DialogueDatabaseSO database = LoadOrCreateAsset<DialogueDatabaseSO>(DialogueDatabasePath);
            ConfigureDatabase(database, engineerGraph, terminalGraph);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            BootstrapSampleScene(database, engineerNpcData, engineerGift);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Selection.activeObject = database;
            Debug.Log("[SpaceLifeDialogueSampleBootstrap] Task 7 sample slice bootstrapped: assets updated and SampleScene wired for 1 NPC + 1 terminal dialogue validation.");
        }

        private static void BootstrapSampleScene(DialogueDatabaseSO database, NPCDataSO engineerNpcData, ItemSO engineerGift)
        {
            Transform sampleRoot = GetOrCreateSampleSliceRoot();

            DialogueUIPresenter dialogueUi = UnityEngine.Object.FindAnyObjectByType<DialogueUIPresenter>();
            GiftUIPresenter giftUi = UnityEngine.Object.FindAnyObjectByType<GiftUIPresenter>();
            GiftInventory giftInventory = GetOrCreateSingleton<GiftInventory>(sampleRoot, nameof(GiftInventory));
            DialogueServiceRouter router = GetOrCreateSingleton<DialogueServiceRouter>(sampleRoot, RouterObjectName);
            SpaceLifeDialogueCoordinator coordinator = GetOrCreateSingleton<SpaceLifeDialogueCoordinator>(sampleRoot, CoordinatorObjectName);
            SpaceLifeDialogueSampleDebugControls debugControls = GetOrCreateSingleton<SpaceLifeDialogueSampleDebugControls>(sampleRoot, SampleDebugObjectName);

            SeedGiftInventory(giftInventory, engineerGift);
            ConfigureRouter(router, giftUi);
            ConfigureCoordinator(coordinator, database, dialogueUi, router);
            ConfigureSampleDebugControls(debugControls);

            NPCController npc = GetOrCreateSampleNpc(sampleRoot, engineerNpcData);
            TerminalDialogueInteractor terminal = GetOrCreateSampleTerminal(sampleRoot);

            npc.transform.position = new Vector3(4f, -2f, 0f);
            terminal.transform.position = new Vector3(7f, -2f, 0f);

            if (dialogueUi == null)
            {
                Debug.LogError("[SpaceLifeDialogueSampleBootstrap] DialogueUIPresenter was not found in SampleScene. Run the existing SpaceLife UI setup before validating the sample slice.");
            }

            if (giftUi == null)
            {
                Debug.LogError("[SpaceLifeDialogueSampleBootstrap] GiftUIPresenter was not found in SampleScene. OpenGift will not be playable until the existing GiftUIPresenter scene object is restored.");
            }
        }

        private static void ConfigureRouter(DialogueServiceRouter router, GiftUIPresenter giftUi)
        {
            if (router == null)
            {
                return;
            }

            SetField(router, "_giftUI", giftUi);
            EditorUtility.SetDirty(router);
        }

        private static void ConfigureCoordinator(SpaceLifeDialogueCoordinator coordinator, DialogueDatabaseSO database, DialogueUIPresenter dialogueUi, DialogueServiceRouter router)
        {
            if (coordinator == null)
            {
                return;
            }

            SetField(coordinator, "_dialogueDatabase", database);
            SetField(coordinator, "_dialogueUI", dialogueUi);
            SetField(coordinator, "_serviceRouter", router);
            EditorUtility.SetDirty(coordinator);
        }

        private static void ConfigureSampleDebugControls(SpaceLifeDialogueSampleDebugControls debugControls)
        {
            if (debugControls == null)
            {
                return;
            }

            SetField(debugControls, "_saveSlot", 0);
            EditorUtility.SetDirty(debugControls);
        }

        private static void SeedGiftInventory(GiftInventory inventory, ItemSO giftItem)
        {
            if (inventory == null || giftItem == null)
            {
                return;
            }

            List<ItemSO> items = inventory.Items;
            if (!items.Contains(giftItem))
            {
                Undo.RecordObject(inventory, "Seed SpaceLife sample gift inventory");
                items.Add(giftItem);
                EditorUtility.SetDirty(inventory);
            }
        }

        private static NPCController GetOrCreateSampleNpc(Transform parent, NPCDataSO engineerNpcData)
        {
            GameObject npcObject = GetOrCreateChild(parent, SampleNpcName);
            EnsureComponent<Interactable>(npcObject);
            EnsureTriggerCollider<CapsuleCollider2D>(npcObject);
            NPCController npc = EnsureComponent<NPCController>(npcObject);
            TextMesh label = EnsureTextLabel(npcObject.transform, "工程师", Color.cyan);
            label.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            SetField(npc, "_npcData", engineerNpcData);
            EditorUtility.SetDirty(npc);
            return npc;
        }

        private static TerminalDialogueInteractor GetOrCreateSampleTerminal(Transform parent)
        {
            GameObject terminalObject = GetOrCreateChild(parent, SampleTerminalName);
            EnsureComponent<Interactable>(terminalObject);
            EnsureTriggerCollider<BoxCollider2D>(terminalObject);
            TerminalDialogueInteractor terminal = EnsureComponent<TerminalDialogueInteractor>(terminalObject);
            TextMesh label = EnsureTextLabel(terminalObject.transform, "舰载AI终端", Color.green);
            label.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            SetField(terminal, "_ownerId", TerminalOwnerId);
            SetField(terminal, "_displayName", "舰载 AI 终端");
            SetField(terminal, "_ownerType", DialogueOwnerType.Terminal);
            EditorUtility.SetDirty(terminal);
            return terminal;
        }

        private static TextMesh EnsureTextLabel(Transform parent, string text, Color color)
        {
            Transform labelTransform = parent.Find("Label");
            GameObject labelObject = labelTransform != null ? labelTransform.gameObject : new GameObject("Label");
            if (labelTransform == null)
            {
                Undo.RegisterCreatedObjectUndo(labelObject, $"Create {parent.name} label");
                labelObject.transform.SetParent(parent, false);
            }

            TextMesh textMesh = EnsureComponent<TextMesh>(labelObject);
            textMesh.text = text;
            textMesh.characterSize = 0.15f;
            textMesh.fontSize = 48;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            return textMesh;
        }

        private static void ConfigureDatabase(DialogueDatabaseSO database, DialogueGraphSO engineerGraph, DialogueGraphSO terminalGraph)
        {
            SetField(database, "_graphs", new List<DialogueGraphSO>
            {
                engineerGraph,
                terminalGraph,
            });

            database.InvalidateCache();
            EditorUtility.SetDirty(database);
        }

        private static void ConfigureEngineerGift(ItemSO item)
        {
            SetField(item, "_itemName", "校准工具包");
            SetField(item, "_description", "一套工程师喜欢的精密校准工具，足够让他愿意多聊两句。\n用于验证 OpenGift 与关系值提升链路。");
            SetField(item, "_baseGiftValue", 20);
            EditorUtility.SetDirty(item);
        }

        private static void ConfigureEngineerNpcData(NPCDataSO npcData, ItemSO likedGift)
        {
            SetField(npcData, "_npcId", EngineerOwnerId);
            SetField(npcData, "_npcName", "工程师");
            SetField(npcData, "_startingRelationship", 10);
            SetField(npcData, "_likedGifts", new List<ItemSO> { likedGift });
            SetField(npcData, "_dislikedGifts", new List<ItemSO>());
            EditorUtility.SetDirty(npcData);
        }

        private static void ConfigureEngineerGraph(DialogueGraphSO graph)
        {
            SetField(graph, "_graphId", "graph_engineer_hub");
            SetField(graph, "_ownerId", EngineerOwnerId);
            SetField(graph, "_ownerType", DialogueOwnerType.Npc);
            SetField(graph, "_entryRules", new List<DialogueEntryRuleData>
            {
                CreateEntryRule("engineer_first_meeting", 20,
                    CreateCondition(DialogueConditionType.FlagAbsent, flagKey: EngineerFirstMetFlag)),
                CreateEntryRule("engineer_private_checkin", 10,
                    CreateCondition(DialogueConditionType.FlagPresent, flagKey: EngineerFirstMetFlag),
                    CreateCondition(DialogueConditionType.RelationshipValue, DialogueCompareOp.GreaterOrEqual, 30)),
                CreateEntryRule("engineer_small_talk", 0)
            });

            SetField(graph, "_nodes", new List<DialogueNodeData>
            {
                CreateNode(
                    nodeId: "engineer_first_meeting",
                    nodeType: DialogueNodeType.Line,
                    speakerName: "工程师",
                    rawText: "新面孔？别紧张，我只是在盯这台快要闹脾气的引擎。先记住我的工位，之后有空再聊。",
                    choices: new List<DialogueChoiceData>
                    {
                        CreateChoice(
                            choiceId: "first_meeting_ack",
                            rawText: "记住了，我之后再来找你。",
                            nextNodeId: "engineer_small_talk",
                            effects: new List<DialogueEffectData>
                            {
                                CreateEffect(DialogueEffectType.SetFlag, flagKey: EngineerFirstMetFlag),
                                CreateEffect(DialogueEffectType.AddRelationship, intValue: 5),
                            })
                    }),
                CreateNode(
                    nodeId: "engineer_small_talk",
                    nodeType: DialogueNodeType.Line,
                    speakerName: "工程师",
                    rawText: "冷却回路勉强稳住了。要是你真想帮忙，我更愿意看到的是靠谱的零件，而不是空话。",
                    choices: new List<DialogueChoiceData>
                    {
                        CreateChoice(
                            choiceId: "engineer_status",
                            rawText: "引擎最近到底哪里最危险？",
                            nextNodeId: "engineer_status_reply"),
                        CreateChoice(
                            choiceId: "engineer_gift",
                            rawText: "我带了点适合你的东西。",
                            exitType: DialogueServiceExitType.OpenGift,
                            exitPayload: EngineerOwnerId)
                    }),
                CreateNode(
                    nodeId: "engineer_status_reply",
                    nodeType: DialogueNodeType.Line,
                    speakerName: "工程师",
                    rawText: "姿态喷口的校准总在飘。等你再证明自己可靠一点，我再告诉你真正棘手的部分。",
                    nextNodeId: "engineer_status_end"),
                CreateNode(
                    nodeId: "engineer_status_end",
                    nodeType: DialogueNodeType.System,
                    speakerName: string.Empty,
                    rawText: "工程师重新埋头回到控制台，只给你留下一句“别把飞船炸了”。"),
                CreateNode(
                    nodeId: "engineer_private_checkin",
                    nodeType: DialogueNodeType.Line,
                    speakerName: "工程师",
                    rawText: "你居然真的记住了我说的话。好吧……如果你还想听，我可以告诉你这艘船真正让我失眠的地方。",
                    choices: new List<DialogueChoiceData>
                    {
                        CreateChoice(
                            choiceId: "engineer_private_topic",
                            rawText: "说吧，我听着。",
                            exitType: DialogueServiceExitType.TriggerRelationshipEvent,
                            exitPayload: "engineer_private_topic"),
                        CreateChoice(
                            choiceId: "engineer_private_leave",
                            rawText: "下次再说。",
                            nextNodeId: "engineer_private_leave_end")
                    }),
                CreateNode(
                    nodeId: "engineer_private_leave_end",
                    nodeType: DialogueNodeType.System,
                    speakerName: string.Empty,
                    rawText: "工程师点点头，把话题暂时压回心里。")
            });

            EditorUtility.SetDirty(graph);
        }

        private static void ConfigureTerminalGraph(DialogueGraphSO graph)
        {
            SetField(graph, "_graphId", "graph_ship_ai_terminal");
            SetField(graph, "_ownerId", TerminalOwnerId);
            SetField(graph, "_ownerType", DialogueOwnerType.Terminal);
            SetField(graph, "_entryRules", new List<DialogueEntryRuleData>
            {
                CreateEntryRule("ship_ai_stage_one", 10,
                    CreateCondition(DialogueConditionType.WorldStage, DialogueCompareOp.GreaterOrEqual, 1)),
                CreateEntryRule("ship_ai_default", 0)
            });

            SetField(graph, "_nodes", new List<DialogueNodeData>
            {
                CreateNode(
                    nodeId: "ship_ai_default",
                    nodeType: DialogueNodeType.System,
                    speakerName: "舰载 AI",
                    rawText: "舰桥日志同步完成。当前仅开放基础维护接口；更深层情报仍受航程阶段限制。",
                    choices: new List<DialogueChoiceData>
                    {
                        CreateChoice(
                            choiceId: "ship_ai_upgrade",
                            rawText: "请求升级接口。",
                            exitType: DialogueServiceExitType.OpenUpgrade,
                            exitPayload: "sample_upgrade_console"),
                        CreateChoice(
                            choiceId: "ship_ai_locked_intel",
                            rawText: "为什么情报还锁着？",
                            nextNodeId: "ship_ai_locked_reply")
                    }),
                CreateNode(
                    nodeId: "ship_ai_locked_reply",
                    nodeType: DialogueNodeType.System,
                    speakerName: "舰载 AI",
                    rawText: "条件未满足：WorldStage < 1。按 F7 可把样板存档写到阶段 1，再回来重新读取终端。"),
                CreateNode(
                    nodeId: "ship_ai_stage_one",
                    nodeType: DialogueNodeType.System,
                    speakerName: "舰载 AI",
                    rawText: "检测到新的航程阶段。额外情报缓存与升级工位已经解锁。",
                    choices: new List<DialogueChoiceData>
                    {
                        CreateChoice(
                            choiceId: "ship_ai_open_intel",
                            rawText: "读取新情报。",
                            exitType: DialogueServiceExitType.OpenIntel,
                            exitPayload: "sample_stage1_intel"),
                        CreateChoice(
                            choiceId: "ship_ai_open_upgrade",
                            rawText: "打开升级接口。",
                            exitType: DialogueServiceExitType.OpenUpgrade,
                            exitPayload: "sample_upgrade_console"),
                        CreateChoice(
                            choiceId: "ship_ai_sync_relationship_event",
                            rawText: "记录这次同步。",
                            exitType: DialogueServiceExitType.TriggerRelationshipEvent,
                            exitPayload: "ship_ai_sync_complete")
                    })
            });

            EditorUtility.SetDirty(graph);
        }

        private static DialogueEntryRuleData CreateEntryRule(string entryNodeId, int priority, params DialogueConditionData[] conditions)
        {
            var rule = new DialogueEntryRuleData();
            SetField(rule, "_entryNodeId", entryNodeId);
            SetField(rule, "_priority", priority);
            SetField(rule, "_conditions", new List<DialogueConditionData>(conditions ?? Array.Empty<DialogueConditionData>()));
            return rule;
        }

        private static DialogueNodeData CreateNode(
            string nodeId,
            DialogueNodeType nodeType,
            string speakerName,
            string rawText,
            string nextNodeId = "",
            List<DialogueChoiceData> choices = null,
            List<DialogueEffectData> effects = null)
        {
            var node = new DialogueNodeData();
            SetField(node, "_nodeId", nodeId);
            SetField(node, "_nodeType", nodeType);
            SetField(node, "_speakerName", speakerName);
            SetField(node, "_rawText", rawText);
            SetField(node, "_nextNodeId", nextNodeId ?? string.Empty);
            SetField(node, "_choices", choices ?? new List<DialogueChoiceData>());
            SetField(node, "_effects", effects ?? new List<DialogueEffectData>());
            return node;
        }

        private static DialogueChoiceData CreateChoice(
            string choiceId,
            string rawText,
            string nextNodeId = "",
            DialogueServiceExitType exitType = DialogueServiceExitType.None,
            string exitPayload = "",
            List<DialogueConditionData> conditions = null,
            List<DialogueEffectData> effects = null)
        {
            var choice = new DialogueChoiceData();
            SetField(choice, "_choiceId", choiceId);
            SetField(choice, "_rawText", rawText);
            SetField(choice, "_conditions", conditions ?? new List<DialogueConditionData>());
            SetField(choice, "_nextNodeId", nextNodeId ?? string.Empty);
            SetField(choice, "_effects", effects ?? new List<DialogueEffectData>());
            SetField(choice, "_exitType", exitType);
            SetField(choice, "_exitPayload", exitPayload ?? string.Empty);
            return choice;
        }

        private static DialogueConditionData CreateCondition(
            DialogueConditionType conditionType,
            DialogueCompareOp compareOp = DialogueCompareOp.GreaterOrEqual,
            int intValue = 0,
            string flagKey = "")
        {
            var condition = new DialogueConditionData();
            SetField(condition, "_conditionType", conditionType);
            SetField(condition, "_compareOp", compareOp);
            SetField(condition, "_intValue", intValue);
            SetField(condition, "_flagKey", flagKey ?? string.Empty);
            return condition;
        }

        private static DialogueEffectData CreateEffect(
            DialogueEffectType effectType,
            int intValue = 0,
            string flagKey = "",
            DialogueServiceExitType serviceExitType = DialogueServiceExitType.None,
            string payload = "")
        {
            var effect = new DialogueEffectData();
            SetField(effect, "_effectType", effectType);
            SetField(effect, "_intValue", intValue);
            SetField(effect, "_flagKey", flagKey ?? string.Empty);
            SetField(effect, "_serviceExitType", serviceExitType);
            SetField(effect, "_payload", payload ?? string.Empty);
            return effect;
        }

        private static Transform GetOrCreateSampleSliceRoot()
        {
            GameObject sceneRoot = GameObject.Find(SpaceLifeSceneRootName);
            Transform parent = sceneRoot != null ? sceneRoot.transform : null;

            GameObject sampleRoot = parent != null ? FindChild(parent, SampleSliceRootName) : null;
            if (sampleRoot == null)
            {
                sampleRoot = new GameObject(SampleSliceRootName);
                Undo.RegisterCreatedObjectUndo(sampleRoot, $"Create {SampleSliceRootName}");
                if (parent != null)
                {
                    sampleRoot.transform.SetParent(parent, false);
                }
            }

            return sampleRoot.transform;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            GameObject existing = FindChild(parent, name);
            if (existing != null)
            {
                return existing;
            }

            GameObject child = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject FindChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return GameObject.Find(name);
            }

            Transform child = parent.Find(name);
            return child != null ? child.gameObject : null;
        }

        private static T GetOrCreateSingleton<T>(Transform parent, string name) where T : MonoBehaviour
        {
            T existing = UnityEngine.Object.FindAnyObjectByType<T>();
            if (existing != null)
            {
                return existing;
            }

            GameObject go = GetOrCreateChild(parent, name);
            return EnsureComponent<T>(go);
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            Undo.AddComponent<T>(go);
            return go.GetComponent<T>();
        }

        private static T EnsureTriggerCollider<T>(GameObject go) where T : Collider2D
        {
            T collider = EnsureComponent<T>(go);
            collider.isTrigger = true;
            return collider;
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureDataFolders()
        {
            EnsureFolder("Assets", "_Data");
            EnsureFolder("Assets/_Data", "SpaceLife");
            EnsureFolder(DataRootFolder, "Dialogue");
            EnsureFolder(DialogueFolder, "Graphs");
            EnsureFolder(DataRootFolder, "NPC");
            EnsureFolder(DataRootFolder, "Items");
        }

        private static void EnsureFolder(string parentFolder, string childFolderName)
        {
            string fullPath = $"{parentFolder}/{childFolderName}";
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentFolder, childFolderName);
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null)
            {
                return;
            }

            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().Name, fieldName);
            }

            field.SetValue(target, value);
        }
    }
}
