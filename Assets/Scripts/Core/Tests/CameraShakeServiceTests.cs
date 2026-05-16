using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ProjectArk.Core.Tests
{
    public sealed class CameraShakeServiceTests
    {
        private static readonly MethodInfo AwakeMethod = typeof(CameraShakeService).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);

        private GameObject _cameraObject;
        private CameraShakeService _service;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _cameraObject = new GameObject("CameraShakeServiceTests_Camera");
            _service = _cameraObject.AddComponent<CameraShakeService>();
            AwakeMethod.Invoke(_service, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_cameraObject != null)
                Object.DestroyImmediate(_cameraObject);

            ServiceLocator.Clear();
        }

        [Test]
        public void Shake_StartsReadableShakeAndRegistersService()
        {
            _service.Shake(0.12f, 0.35f, 28f);

            Assert.That(ServiceLocator.TryGet<CameraShakeService>(), Is.SameAs(_service));
            Assert.That(_service.IsShaking, Is.True);
            Assert.That(_service.RemainingDuration, Is.EqualTo(0.12f).Within(0.0001f));
            Assert.That(_service.CurrentAmplitude, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(_service.CurrentFrequency, Is.EqualTo(28f).Within(0.0001f));
        }

        [Test]
        public void Shake_ClampsNegativeValuesAndIgnoresZeroDuration()
        {
            _service.Shake(0f, 0.35f, 28f);

            Assert.That(_service.IsShaking, Is.False);

            _service.Shake(0.1f, -1f, -5f);

            Assert.That(_service.IsShaking, Is.True);
            Assert.That(_service.CurrentAmplitude, Is.Zero);
            Assert.That(_service.CurrentFrequency, Is.Zero);
        }

        [Test]
        public void Step_StopsShakeAndRestoresBasePositionAfterDuration()
        {
            Vector3 startPosition = _cameraObject.transform.localPosition;
            _service.Shake(0.05f, 0.25f, 20f);

            _service.Step(0.025f);
            Assert.That(_service.IsShaking, Is.True);

            _service.Step(0.05f);

            Assert.That(_service.IsShaking, Is.False);
            Assert.That(_service.RemainingDuration, Is.Zero);
            Assert.That(_cameraObject.transform.localPosition, Is.EqualTo(startPosition));
        }
    }
}
