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
        OnResultReady?.Invoke();
    }

    public static string TrimFluff(string result) {
        string trimmed = result.ToLower();
        trimmed = trimmed.Replace("[pause]", "");
        trimmed = trimmed.Replace("[silence]", "");
        // Some will be broken replacing bits of words
        trimmed = trimmed.Replace("zero", "0");
        trimmed = trimmed.Replace("one", "1");
        trimmed = trimmed.Replace("two", "2");
        trimmed = trimmed.Replace("three", "3");
        trimmed = trimmed.Replace("four", "4");
        trimmed = trimmed.Replace("five", "5");
        trimmed = trimmed.Replace("six", "6");
        trimmed = trimmed.Replace("seven", "7");
        trimmed = trimmed.Replace("eight", "8");
        trimmed = trimmed.Replace("nine", "9");
        trimmed = trimmed.Replace("ten", "10");
        trimmed = trimmed.Trim();
        if (trimmed != "" && trimmed[trimmed.Length-1] == '.') {
            trimmed = trimmed.Remove(trimmed.Length-1);
        }
        return trimmed;
    }

  //private void OnNewSegment(WhisperSegment segment)
  //{
  //    if (!streamSegments || !outputText)
  //        return;

  //    _buffer += segment.Text;
  //    outputText.text = _buffer + "...";
  //}
}