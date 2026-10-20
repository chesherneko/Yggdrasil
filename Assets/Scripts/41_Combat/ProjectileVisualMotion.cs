using UnityEngine;

namespace Combat
{
    /// <summary>
    /// 이동 애니메이션과 투사체 외형의 회전을 분리한다.
    /// </summary>
    public class ProjectileVisualMotion : MonoBehaviour
    {
        private enum MotionMode
        {
            None,
            Spin,
            FixedWorldAngle
        }

        [SerializeField] private MotionMode mode;
        [SerializeField] private float angularSpeed;
        [SerializeField] private float fixedWorldAngle;

        private float spinAngle;
        private Quaternion initialLocalRotation;

        private void Awake()
        {
            initialLocalRotation = transform.localRotation;
        }

        public void ConfigureSpin(float degreesPerSecond)
        {
            mode = MotionMode.Spin;
            angularSpeed = degreesPerSecond;
        }

        public void ConfigureFixedWorldAngle(float angle)
        {
            mode = MotionMode.FixedWorldAngle;
            fixedWorldAngle = angle;
        }

        private void LateUpdate()
        {
            switch (mode)
            {
                case MotionMode.Spin:
                    spinAngle = Mathf.Repeat(spinAngle + angularSpeed * Time.deltaTime, 360f);
                    transform.localRotation =
                        initialLocalRotation * Quaternion.Euler(0f, 0f, spinAngle);
                    break;

                case MotionMode.FixedWorldAngle:
                    transform.rotation = Quaternion.Euler(0f, 0f, fixedWorldAngle);
                    break;
            }
        }
    }
}
