using UnityEngine;
using System.Collections;
using System;
using System.Threading.Tasks;

public class SharedFunctions
{
  public static IEnumerator Retry(Func<Task> a, int sec = 1)
  {
    Logger.AppendLog($"retrying #{a}...", false);
    yield return new WaitForSeconds(sec);
    a();
  }

  public static string GetUnixTime()
  {
    DateTime epochStart = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    long cur_time = (long)(DateTime.UtcNow - epochStart).TotalMilliseconds;
    return cur_time.ToString();
  }
}
