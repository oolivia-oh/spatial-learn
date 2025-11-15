using System.Threading.Tasks;
using UnityEngine;
using Whisper;
using Whisper.Utils;

public class SpeechManager : MonoBehaviour
{
    private WhisperManager whisper;
    private MicrophoneRecord microphoneRecord;

  //private string _buffer;
    public string result = "";
    public delegate void Notify();
    public event Notify OnResultReady;

    private void Awake()
    {
        whisper = gameObject.GetComponent<WhisperManager>();
        //whisper.OnNewSegment += OnNewSegment;

        microphoneRecord = gameObject.GetComponent<MicrophoneRecord>();
        microphoneRecord.OnRecordStop += OnRecordStop;
    }

    public void Record() {
        if (!microphoneRecord.IsRecording) microphoneRecord.StartRecord();
    }

    public bool StopRecord() {
        if (microphoneRecord.IsRecording) microphoneRecord.StopRecord();
        return microphoneRecord.IsRecording;
    }
    
    private async void OnRecordStop(AudioChunk recordedAudio)
    {
   //   _buffer = "";

        WhisperResult whisperResult = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
        if (whisperResult == null) {
            Debug.Log($"early exi");
            return;
        }
        result = whisperResult.Result;
        Debug.Log($"Order: {result}");
        OnResultReady?.Invoke();
    }
    
  //private void OnNewSegment(WhisperSegment segment)
  //{
  //    if (!streamSegments || !outputText)
  //        return;

  //    _buffer += segment.Text;
  //    outputText.text = _buffer + "...";
  //}
}