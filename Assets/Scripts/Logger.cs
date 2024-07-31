using UnityEngine;
using TMPro;
using System.IO;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;


public class Logger : MonoBehaviour
{
  public static TextMeshPro tmp;
  private static int num_lines = 0;
  private static int max_num_lines = 10;

  private static bool IsTracking = false;

  [SerializeField]
  private GameObject[] ChildItems;
  public void HideLogger()
  {
    foreach (var item in ChildItems)
      item.SetActive(false);
  }

  private static void CreateFile(string path)
  {
    Logger.AppendLog($"Logger: Creating file at: {path}", false);
    try
    {
      using (File.Create(path)) { };
    }
    catch (System.Exception e)
    {
      Logger.AppendLogError($"Unable to create file: {e.Message}");
    }
  }

  private static string EventFilePath;

  public static void StartTracking(string DirName)
  {
    string DirPath = Path.Combine(Application.persistentDataPath, DirName);
    Logger.AppendLog($"DirPath = {DirPath}", false);
    Directory.CreateDirectory(DirPath);

    EventFilePath = Path.Combine(DirPath, SharedFunctions.GetUnixTime() + "-events.csv");
    CreateFile(EventFilePath);

    string[] eventHeaderInfo = { "timestamp",
                                 "event" };
    WriteToFile(EventFilePath, eventHeaderInfo);

    IsTracking = true;
  }

  private static void WriteToFile(string path, string[] content)
  {
    string output = "";
    for (int i = 0; i < content.Length; i++)
    {
      output += "\"" + content[i] + "\"";
      if (i != (content.Length - 1))
      {
        output += ",";
      }
    }
    output += "\n";
    try
    {
      File.AppendAllText(path, output);
    }
    catch (System.Exception e)
    {
      Logger.AppendLogError($"Unable to write to log file: {e.Message}");
    }
  }

  public static void StopTracking()
  {
    Logger.AppendLog("Stopping Data Tracking...");
    IsTracking = false;
  }

  public static void AppendLog(string msg, bool display = true, bool write = true)
  {
    Debug.Log(msg);
    if (display)
    {
      if (num_lines >= max_num_lines)
      {
        tmp.text = "";
        num_lines = 0;
      }
      tmp.text += msg + "\n";
      num_lines += 1;
    }
    if (!IsTracking || !write) return;
    string[] content = { SharedFunctions.GetUnixTime(),
                         msg };
    WriteToFile(EventFilePath, content);
  }

  public static void AppendLogWarning(string msg, bool display = false, bool write = true)
  {
    Debug.LogWarning(msg);
    if (display)
    {
      if (num_lines >= max_num_lines)
      {
        tmp.text = "";
        num_lines = 0;
      }
      tmp.text += "Warning: " + msg + "\n";
      num_lines += 1;
    }
    if (!IsTracking || !write) return;
    string[] content = { SharedFunctions.GetUnixTime(),
                         "Warning: " + msg};
    WriteToFile(EventFilePath, content);
  }

  public static void AppendLogError(string msg, bool display = false, bool write = true)
  {
    Debug.LogError(msg);
    if (display)
    {
      if (num_lines >= max_num_lines)
      {
        tmp.text = "";
        num_lines = 0;
      }
      tmp.text += "Error: " + msg + "\n";
      num_lines += 1;
    }
    if (!IsTracking || !write) return;
    string[] content = { SharedFunctions.GetUnixTime(),
                         "Error: " + msg };
    WriteToFile(EventFilePath, content);
  }

  // Start is called before the first frame update
  void Start()
  {
    tmp = GetComponentInChildren<TextMeshPro>();
  }
}
