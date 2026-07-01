using UnityEngine;
using UnityEngine.SceneManagement;

public class GhostRunRecorder : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public DeathScript deathScript;
    public PlayerSkinSwitcher skinSwitcher;

    [Header("Recording")]
    public bool recordOnStart = true;
    public bool saveOnDisable = true;
    public float sampleInterval = 0.05f;

    private GhostRunData currentRun = new GhostRunData();
    private float elapsedTime = 0f;
    private float sampleTimer = 0f;
    private bool isRecording = false;

    public bool IsRecording => isRecording;
    public float ElapsedTime => elapsedTime;
    public GhostRunData CurrentRun => currentRun;

    void Start()
    {
        ResolveReferences();

        if (recordOnStart)
            BeginRecording();
    }

    void Update()
    {
        if (!isRecording || target == null)
            return;

        float interval = Mathf.Max(0.01f, sampleInterval);

        elapsedTime += Time.deltaTime;
        sampleTimer += Time.deltaTime;

        if (sampleTimer < interval)
            return;

        sampleTimer = 0f;
        RecordFrame();
    }

    void OnDisable()
    {
        if (isRecording)
            StopRecording(saveOnDisable);
    }

    public void BeginRecording()
    {
        ResolveReferences();

        currentRun.Clear();
        currentRun.sceneName = SceneManager.GetActiveScene().name;
        currentRun.skinIndex = GetSkinIndex();

        elapsedTime = 0f;
        sampleTimer = 0f;
        isRecording = true;

        RecordFrame();
    }

    public GhostRunData StopRecording(bool save)
    {
        if (!isRecording)
            return currentRun;

        isRecording = false;
        currentRun.duration = elapsedTime;

        if (save)
            GhostRunStorage.SaveForCurrentScene(currentRun);

        return currentRun;
    }

    public GhostRunData GetCurrentRun()
    {
        return currentRun;
    }

    public RacePlayerSnapshot GetCurrentSnapshot()
    {
        if (target == null)
            ResolveReferences();

        if (target == null)
            return null;

        return new RacePlayerSnapshot(
            elapsedTime,
            target.position,
            target.rotation,
            IsAlive(),
            false,
            GetSkinIndex()
        );
    }

    private void ResolveReferences()
    {
        if (target == null)
            target = transform;

        if (deathScript == null)
            deathScript = GetComponent<DeathScript>();

        if (skinSwitcher == null)
            skinSwitcher = GetComponentInChildren<PlayerSkinSwitcher>(true);
    }

    private void RecordFrame()
    {
        RacePlayerSnapshot snapshot = GetCurrentSnapshot();
        if (snapshot != null)
            currentRun.frames.Add(snapshot.ToGhostFrame());
    }

    private bool IsAlive()
    {
        return deathScript == null || !deathScript.IsDead();
    }

    private int GetSkinIndex()
    {
        return skinSwitcher != null ? skinSwitcher.GetCurrentSkinIndex() : 0;
    }
}
