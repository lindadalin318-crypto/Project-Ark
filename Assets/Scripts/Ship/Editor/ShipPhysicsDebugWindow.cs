using UnityEditor;
using UnityEngine;

namespace ProjectArk.Ship.Editor
{
    /// <summary>
    /// Play Mode 实时物理调参窗口。
    /// 菜单：ProjectArk > Ship > Physics Debug
    ///
    /// 对照 Galactic Glitch 实际感受，逐项调整 ShipStatsSO 参数。
    /// 窗口显示实时速度、角速度，并提供推荐参数范围说明。
    /// </summary>
    public class ShipPhysicsDebugWindow : EditorWindow
    {
        private ShipStatsSO _stats;
        private ShipMotor   _motor;
        private Rigidbody2D _rb;

        private float _peakSpeed;
        private float _peakAngularVelocity;
        private bool  _autoFind = true;

        [MenuItem("ProjectArk/Ship/Physics Debug")]
        public static void Open()
        {
            var w = GetWindow<ShipPhysicsDebugWindow>("Ship Physics Debug");
            w.minSize = new Vector2(340, 580);
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            // ── 自动查找 ShipMotor ──
            if (_autoFind && Application.isPlaying && _motor == null)
            {
                _motor = Object.FindAnyObjectByType<ShipMotor>();
                if (_motor != null)
                {
                    _rb    = _motor.GetComponent<Rigidbody2D>();
                    _stats = _motor.GetComponent<ShipAiming>() != null
                        ? null
                        : null;
                    // 通过反射拿 _stats
                    var field = typeof(ShipMotor).GetField("_stats",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                        _stats = field.GetValue(_motor) as ShipStatsSO;
                }
            }

            EditorGUILayout.Space(4);
            DrawHeader("🎮 实时物理状态", Color.cyan);

            bool isPlaying = Application.isPlaying;
            if (!isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后查看实时数值。", MessageType.Info);
            }
            else if (_motor == null)
            {
                EditorGUILayout.HelpBox("场景中未找到 ShipMotor。", MessageType.Warning);
            }
            else
            {
                DrawLiveStats();
            }

            EditorGUILayout.Space(8);
            DrawHeader("⚙️ 参数调整（直接修改 SO）", Color.yellow);

            _stats = (ShipStatsSO)EditorGUILayout.ObjectField("ShipStatsSO", _stats, typeof(ShipStatsSO), false);

            if (_stats != null)
                DrawParamEditor();

            EditorGUILayout.Space(8);
            DrawHeader("📊 GG 手感对照参考", Color.green);
            DrawGGReference();
        }

        private void DrawLiveStats()
        {
            if (_rb == null) return;

            float speed  = _rb.linearVelocity.magnitude;
            float angVel = Mathf.Abs(_rb.angularVelocity);

            if (speed > _peakSpeed)        _peakSpeed = speed;
            if (angVel > _peakAngularVelocity) _peakAngularVelocity = angVel;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("线速度 (units/s)",  speed);
                EditorGUILayout.FloatField("峰值速度",          _peakSpeed);
                EditorGUILayout.FloatField("角速度 (deg/s)",    angVel);
                EditorGUILayout.FloatField("峰值角速度",        _peakAngularVelocity);
                EditorGUILayout.FloatField("归一化速度",        _motor.NormalizedSpeed);
                EditorGUILayout.Toggle("IsBoosting",           _motor.IsBoosting);
            }

            if (GUILayout.Button("重置峰值"))
            {
                _peakSpeed = 0f;
                _peakAngularVelocity = 0f;
            }
        }

        private void DrawParamEditor()
        {
            var so = new SerializedObject(_stats);
            so.Update();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("旋转", EditorStyles.boldLabel);
            DrawProp(so, "_angularAcceleration", "角加速度",    "GG感：800~1200，越高转向越猛");
            DrawProp(so, "_maxRotationSpeed",    "最大角速度",  "GG感：300~500 deg/s");
            DrawProp(so, "_angularDrag",         "角阻力",      "GG感：5~12，越高惯性越少");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("前向推力", EditorStyles.boldLabel);
            DrawProp(so, "_forwardAcceleration", "前向加速度",  "GG感：15~30 units/s²");
            DrawProp(so, "_maxSpeed",            "最大速度",    "GG感：8~14 units/s");
            DrawProp(so, "_linearDrag",          "线性阻力",    "GG感：2~5，越小越「太空滑翔」");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Boost", EditorStyles.boldLabel);
            DrawProp(so, "_boostImpulse",            "冲量大小",    "GG感：15~25");
            DrawProp(so, "_boostDuration",           "持续时长",    "GG感：0.2~0.4s");
            DrawProp(so, "_boostMaxSpeedMultiplier", "速度倍率",    "GG感：1.8~2.5×");
            DrawProp(so, "_boostCooldown",           "冷却",        "GG感：1.0~1.5s");

            if (so.ApplyModifiedProperties())
            {
                if (_rb != null && Application.isPlaying)
                {
                    // 运行时同步 drag
                    _rb.linearDamping  = _stats.LinearDrag;
                    _rb.angularDamping = _stats.AngularDrag;
                }
            }
        }

        private void DrawProp(SerializedObject so, string propName, string label, string hint)
        {
            var prop = so.FindProperty(propName);
            if (prop == null) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(prop, new GUIContent(label));
            }
            var style = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            style.normal.textColor = new Color(0.6f, 0.8f, 0.6f);
            EditorGUILayout.LabelField(hint, style);
        }

        private void DrawGGReference()
        {
            var style = new GUIStyle(EditorStyles.helpBox) { wordWrap = true, fontSize = 11 };
            EditorGUILayout.LabelField(
                "GG 飞船手感核心特征：\n\n" +
                "1. 「转向有重量感」\n" +
                "   - 高速时不能立即反向，需要先旋转再加速\n" +
                "   - 对应：angularAcceleration 高但 angularDrag 也高（加速快衰减也快）\n\n" +
                "2. 「前进有惯性但不飘」\n" +
                "   - 松油门后大约 1~2 秒停下（不是瞬停也不是永远飘）\n" +
                "   - 对应：linearDrag ≈ 3~4\n\n" +
                "3. 「Boost 是爆发冲刺」\n" +
                "   - 按下瞬间飞出去，大约 0.3s 内明显超速，然后回落\n" +
                "   - 对应：boostImpulse ≈ 18~22，linearDrag 自然衰减\n\n" +
                "4. 调参建议顺序：\n" +
                "   linearDrag → maxSpeed → angularAcceleration → boostImpulse",
                style
            );
        }

        private void DrawHeader(string title, Color color)
        {
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = color;
            EditorGUILayout.LabelField(title, style);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color * 0.4f);
            EditorGUILayout.Space(2);
        }
    }
}
