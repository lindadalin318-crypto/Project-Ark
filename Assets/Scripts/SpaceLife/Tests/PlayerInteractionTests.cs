using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.SpaceLife.Tests
{
    [TestFixture]
    public class PlayerInteractionTests
    {
        private readonly List<Object> _createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void FindNearestInteractable_UsesDistanceFallback_WhenNoTriggerContactsRecorded()
        {
            var playerRoot = new GameObject("Player");
            playerRoot.AddComponent<CircleCollider2D>();
            var playerInteraction = playerRoot.AddComponent<PlayerInteraction>();
            playerRoot.transform.position = Vector3.zero;
            _createdObjects.Add(playerRoot);

            var interactableRoot = new GameObject("Engineer");
            var interactable = interactableRoot.AddComponent<Interactable>();
            interactableRoot.transform.position = Vector3.right;
            _createdObjects.Add(interactableRoot);

            InvokePrivateMethod(playerInteraction, "FindNearestInteractable");
            Interactable nearest = GetPrivateField<Interactable>(playerInteraction, "_nearestInteractable");

            Assert.AreSame(interactable, nearest,
                "PlayerInteraction 应该能基于距离找到范围内最近的 Interactable，而不是完全依赖 trigger 列表。"
            );
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                Assert.Fail($"Method '{methodName}' not found on {target.GetType().Name}.");
            }

            method.Invoke(target, null);
        }

        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Assert.Fail($"Field '{fieldName}' not found on {target.GetType().Name}.");
            }

            return field.GetValue(target) as T;
        }
    }
}
