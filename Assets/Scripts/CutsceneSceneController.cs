using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class CutsceneSceneController : MonoBehaviour
{
    public enum DebugCutscenePick { Intro, End }

    [System.Serializable]
    public class CutsceneChoice
    {
        [Tooltip("If player came from this build index, choose this cutscene.")]
        public int comingFromBuildIndex = -1;

        [Tooltip("Clip to play for this route.")]
        public VideoClip clip;

        [Tooltip("Where to go after this clip finishes.")]
        public int nextSceneBuildIndex = -1;

        [Tooltip("If clip length can't be read, wait this many seconds.")]
        public float fallbackSeconds = 8f;
    }

    [Header("Shared UI/Video")]
    public GameObject cutsceneUIRoot;
    public VideoPlayer videoPlayer;

    [Header("Start Delay + Loading UI")]
    [Tooltip("Wait this many seconds after the scene starts before playing the video.")]
    public float startDelaySeconds = 0f;

    [Tooltip("UI GameObject (Image/Panel/Text) that shows 'Loading...' during the start delay.")]
    public GameObject loadingUI;

    [Header("Routes")]
    public CutsceneChoice intro;
    public CutsceneChoice end;

    [Header("Debug Fallback (when PreviousSceneBuildIndex is -1)")]
    public bool useDebugFallbackIfNoPreviousScene = true;
    public DebugCutscenePick debugPick = DebugCutscenePick.Intro;

    [Header("Timing")]
    public bool useRealtimeWait = true;

    [Header("Skip")]
    public bool allowSkip = true;
    public KeyCode skipKey = KeyCode.Space;

    private bool _finished;
    private CutsceneChoice _active;

    private void Start()
    {
        int prev = SceneLoader.PreviousSceneBuildIndex;

        if (prev == -1 && useDebugFallbackIfNoPreviousScene)
            _active = (debugPick == DebugCutscenePick.Intro) ? intro : end;
        else
            _active = Pick(prev);

        if (_active == null)
        {
            Debug.LogError("[CutsceneSceneController] No cutscene choice configured for this entry.");
            return;
        }

        StartCoroutine(PlayThenLoad());
    }

    private void Update()
    {
        if (_finished) return;
        if (allowSkip && Input.GetKeyDown(skipKey))
            Finish(stopVideo: true);
    }

    private CutsceneChoice Pick(int prev)
    {
        if (intro != null && intro.comingFromBuildIndex == prev) return intro;
        if (end != null && end.comingFromBuildIndex == prev) return end;
        return null;
    }

    private IEnumerator PlayThenLoad()
    {
        _finished = false;

        if (cutsceneUIRoot != null) cutsceneUIRoot.SetActive(true);

        if (videoPlayer == null)
        {
            Debug.LogError("[CutsceneSceneController] VideoPlayer not assigned.");
            yield break;
        }

        if (_active.clip == null)
        {
            Debug.LogError("[CutsceneSceneController] Active clip is not assigned.");
            yield break;
        }

        // Show loading UI during delay
        if (loadingUI != null) loadingUI.SetActive(startDelaySeconds > 0f);

        if (startDelaySeconds > 0f)
        {
            if (useRealtimeWait) yield return new WaitForSecondsRealtime(startDelaySeconds);
            else yield return new WaitForSeconds(startDelaySeconds);
        }

        // Hide loading UI when video starts
        if (loadingUI != null) loadingUI.SetActive(false);

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = _active.clip;
        videoPlayer.Play();

        double duration = _active.clip.length;
        float waitSeconds = (duration > 0) ? (float)duration : _active.fallbackSeconds;

        if (useRealtimeWait) yield return new WaitForSecondsRealtime(waitSeconds);
        else yield return new WaitForSeconds(waitSeconds);

        Finish(stopVideo: false);
    }

    private void Finish(bool stopVideo)
    {
        if (_finished) return;
        _finished = true;

        if (videoPlayer != null && stopVideo)
            videoPlayer.Stop();

        if (loadingUI != null) loadingUI.SetActive(false);
        if (cutsceneUIRoot != null) cutsceneUIRoot.SetActive(false);

        if (_active.nextSceneBuildIndex < 0)
        {
            Debug.LogError("[CutsceneSceneController] nextSceneBuildIndex not set for active cutscene.");
            return;
        }

        SceneLoader.LoadByIndexPublic(_active.nextSceneBuildIndex);
    }
}
