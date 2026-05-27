using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class BackendHealthService : MonoBehaviour
{
    private const string HEALTH_URL = "https://slot-dogs.onrender.com/health";

    public IEnumerator PingLoop(Action onSuccess, Action<int> onAttempt)
    {
        int attempt = 0;
        while (true)
        {
            attempt++;
            onAttempt?.Invoke(attempt);

            using (UnityWebRequest ping = UnityWebRequest.Get(HEALTH_URL))
            {
                ping.timeout = 10;
                yield return ping.SendWebRequest();

                if (ping.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke();
                    yield break;
                }
            }
            yield return new WaitForSeconds(3f);
        }
    }
}
