using UnityEngine;

namespace CodeBase.Infrastructure.Logic
{
    public class LoadingCurtain : MonoBehaviour
    {
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}