using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.Windows.WebCam;
using System.Linq;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using UnityEngine.XR;
using MixedReality.Toolkit.Input;
using Unity.VisualScripting;

public static class VideoRecorder
{
  private static string VideoDirPath;

  private static VideoCapture m_VideoCapture = null;


  public static void StartRecording(string _VideoDirPath)
  {
    VideoDirPath = _VideoDirPath;
    if (Application.isEditor)
    {
      Logger.AppendLogWarning("I'm not gonna record your webcam while running the app in the editor...");
      return;
    }
    VideoCapture.CreateAsync(true, OnVideoCaptureCreated);
  }

  public static void StopRecording()
  {
    m_VideoCapture?.StopRecordingAsync(OnStoppedRecordingVideo);
  }

  private static void OnVideoCaptureCreated(VideoCapture videoCapture)
  {
    if (videoCapture != null)
    {
      m_VideoCapture = videoCapture;

      Resolution cameraResolution = VideoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).ElementAtOrDefault(2);
      foreach (var elem in VideoCapture.SupportedResolutions)
      {
        Debug.LogFormat("Supported res: {0} x {1}", elem.width, elem.height);
        foreach (var elem2 in VideoCapture.GetSupportedFrameRatesForResolution(elem))
        {
          Debug.LogFormat("Supported framerate: {0}", elem2);
        }
      }
      float cameraFramerate = VideoCapture.GetSupportedFrameRatesForResolution(cameraResolution).OrderByDescending((fps) => fps).First();
      CameraParameters cameraParameters = new()
      {
        hologramOpacity = 1.0f,
        frameRate = cameraFramerate,
        cameraResolutionWidth = cameraResolution.width,
        cameraResolutionHeight = cameraResolution.height,
        pixelFormat = CapturePixelFormat.BGRA32
      };

      m_VideoCapture.StartVideoModeAsync(cameraParameters,
                                          VideoCapture.AudioState.ApplicationAndMicAudio,
                                          OnStartedVideoCaptureMode);
    }
    else
    {
      Logger.AppendLogWarning("Failed to create VideoCapture Instance");
    }
  }

  private static void OnStartedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
  {
    if (result.success)
    {
      string filename = string.Format("{0}.mp4", SharedFunctions.GetUnixTime());
      // string dirPath = Path.Combine(basePath, "TrackedData");
      if (!Directory.Exists(VideoDirPath))
        Directory.CreateDirectory(VideoDirPath);

      string filepath = Path.Combine(VideoDirPath, filename);
      Logger.AppendLog($"Video File Path: {filepath}", false);

      m_VideoCapture.StartRecordingAsync(filepath, OnStartedRecordingVideo);
    }
  }

  private static void OnStartedRecordingVideo(VideoCapture.VideoCaptureResult result)
  {
    Logger.AppendLog("video recording started");
  }

  private static void OnStoppedRecordingVideo(VideoCapture.VideoCaptureResult result)
  {
    Logger.AppendLog("video recording stopped");
    m_VideoCapture.StopVideoModeAsync(OnStoppedVideoCaptureMode);
  }

  private static void OnStoppedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
  {
    m_VideoCapture.Dispose();
    m_VideoCapture = null;
  }
}

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

  private static string LogFilePath, EventFilePath;
  private static HandsAggregatorSubsystem aggregator;

  public static void StartTracking(string DirName)
  {
    string DirPath = Path.Combine(Application.persistentDataPath, DirName);
    Logger.AppendLog($"DirPath = {DirPath}", false);
    Directory.CreateDirectory(DirPath);

    LogFilePath = Path.Combine(DirPath, SharedFunctions.GetUnixTime() + "-data.csv");
    CreateFile(LogFilePath);

    string[] headerInfo = { "timestamp",

                            "rif_pos",
                            "rif_rot",

                            "lif_pos",
                            "lif_rot",

                            "lb_pos",
                            "lb_rot",

                            "gaze_origin_pose",
                            "gaze_origin_rot",

                            "gaze_hit_IsRayCast",
                            "gaze_hit_point",
                            "gaze_hit_name"

                          };
    WriteToFile(LogFilePath, headerInfo);
    EventFilePath = Path.Combine(DirPath, SharedFunctions.GetUnixTime() + "-events.csv");
    CreateFile(EventFilePath);

    string[] eventHeaderInfo = { "timestamp",
                                 "event" };
    WriteToFile(EventFilePath, eventHeaderInfo);

    VideoRecorder.StartRecording(DirPath);
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
    VideoRecorder.StopRecording();
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
    aggregator = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
  }

  [SerializeField]
  private FuzzyGazeInteractor GazeInteractor;
  [SerializeField]
  private InteractorDwellManager DwellManager;

  void FixedUpdate()
  {
    if (!IsTracking) return;
    try
    {
      bool rifPoseIsValid = aggregator.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.RightHand, out HandJointPose rifPose);
      bool lifPoseIsValid = aggregator.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.LeftHand, out HandJointPose lifPose);
      var gazeTransform = GazeInteractor.rayOriginTransform;
      var lb = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().SpawnedObj;

      string[] content = {
                         SharedFunctions.GetUnixTime(),

                         !rifPoseIsValid ? "null" : rifPose.Position.ToString("F6"),
                         !rifPoseIsValid ? "null" : rifPose.Rotation.normalized.ToString("F6"),

                         !lifPoseIsValid ? "null" : lifPose.Position.ToString("F6"),
                         !lifPoseIsValid ? "null" : lifPose.Rotation.normalized.ToString("F6"),

                         (lb == null || lb.IsDestroyed()) ? "null" : lb.transform.position.ToString("F6"),
                         (lb == null || lb.IsDestroyed()) ? "null" : lb.transform.rotation.normalized.ToString("F6"),

                         gazeTransform.position.ToString("F6"),
                         gazeTransform.rotation.normalized.ToString("F6"),

                         GazeInteractor.PreciseHitResult.IsRaycast.ToString(),
                         GazeInteractor.PreciseHitResult.raycastHit.point.ToString("F6"),
                         GazeInteractor.PreciseHitResult.targetInteractable?.transform.gameObject.name
                        };
      WriteToFile(LogFilePath, content);
    }
    catch (System.Exception e)
    {
      Logger.AppendLogWarning($"Skipping log for 1 frame: {e.Message}");
    }
  }
}
