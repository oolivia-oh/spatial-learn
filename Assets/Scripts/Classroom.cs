using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using Whisper.Utils;

public enum TeacherMode {
    AboutChairQuestion,
    ChairClickQuestion,
    MatchChairQuestion,
    RelatedInfoQuestion,
    Explaining
} 

public class Classroom : MonoBehaviour
{
    public string chairsFileName;
    public string primaryKey;
    public Vector2 startPoint;
    public float spacingMultiplier;
    public Lesson lesson;
    public ColorPalette colors;
    private SelectableGroup choiceGroup;
    private SelectableGroup mainGroup;
    private VisualElement root;
    private Label teacher;
    private TextField teacherEar;
    private Button answerKeyMenuButton;
    private List<string> allAttributes;
    private ToggleButton speechInputToggle;
    private ToggleButton lowestLevelOverride;
    private UnsignedIntegerField lowestLevelField;
    private Chair answerChair;
    private string answer; // DEPRECATED
    private string answerKey;
    private TeacherMode teacherMode; 
    public int episodeIndex = 0;
    public bool teachAscending = true;
    public int toTeach = 6;
    public bool subordinate = true;
    public SpeechManager speechManager;
    public Classroom classroom;

    // Start is called before the first frame update
    void Start() {
        GlobalConfig.Init(colors);
        root = GetComponent<UIDocument>().rootVisualElement;
        VisualElement configContainer = new VisualElement();
        configContainer.style.flexDirection = FlexDirection.Column;
        configContainer.style.alignItems = Align.FlexEnd;
        answerKeyMenuButton = new Button();
        answerKeyMenuButton.style.width = 120;
        answerKeyMenuButton.clicked += Dropdown;
        VisualElement lowestLevelContainer = new VisualElement();
        lowestLevelOverride = new ToggleButton();
        lowestLevelOverride.text = "Override\nLowest Level";
        lowestLevelOverride.style.width = 110;
        lowestLevelField = new UnsignedIntegerField();
        lowestLevelContainer.Add(lowestLevelOverride);
        lowestLevelContainer.Add(lowestLevelField);
        speechInputToggle = new ToggleButton();
        speechInputToggle.text = "Speech Input";
        speechInputToggle.style.width = 110;
        configContainer.Add(answerKeyMenuButton);
        configContainer.Add(lowestLevelContainer);
        configContainer.Add(speechInputToggle);
        root.Add(configContainer);
        VisualElement container = new VisualElement();
        Chair.Init(startPoint, spacingMultiplier, 90, 90, primaryKey);
        container.style.justifyContent = Justify.Center;
        container.style.flexDirection = FlexDirection.Column;
        container.style.alignItems = Align.Center;
        teacher = new Label("Hello");
        teacherEar = new TextField();
        choiceGroup = new SelectableGroup(new List<Chair>(), 0);
        Debug.Log($"Contains test: {"[silence] Laura.".Contains("Laura")}");

        teacherEar.visible = false;
        container.Add(teacher);
        container.Add(teacherEar);
        root.Add(container);
        LoadCSV(chairsFileName);
        answerKey = allAttributes[0];
        answerKeyMenuButton.text = answerKey;
        teachAscending = false;
        speechManager.OnResultReady += Accept;
        //AskAboutChairQuestion("Who sits here?", "firstName", "Mirabel", 4);
        //AskChairClickQuestion("Where does Mirabel sit?", "firstName", "Mirabel");
        AskQuestion(TeacherMode.Explaining, "Hello!");
        if (subordinate) {
            root.visible = false;
        }
    }

    public void Accept() {
        bool allRight = false;
        switch (teacherMode) {
            case TeacherMode.AboutChairQuestion:
                string selectedAnswer = teacherEar.text;
                if (speechInputToggle.On) {
                    selectedAnswer = speechManager.result;
                }
                if (choiceGroup != null && choiceGroup.selected.Count > 0) {
                    selectedAnswer = choiceGroup.selected.Peek().attributes[answerKey];
                    choiceGroup.CheckAnswer(answerKey, selectedAnswer, answer);
                }
                allRight = mainGroup.CheckAnswer(answerKey, answer, selectedAnswer);
                if (allRight) {// BROKEN for multichoice
                    teacherMode = TeacherMode.Explaining;
                    Chair rightChair = mainGroup.GetChair(answerKey, answer);
                    rightChair.histories[answerKey].rightTypedN++;
                    if (rightChair.histories[answerKey].wrongTypedN < rightChair.histories[answerKey].rightTypedN) {
                        mainGroup.teaching.Remove(rightChair);
                    }
                } else {
                    Chair wrongChair = mainGroup.GetChair(answerKey, answer);
                    mainGroup.GetChair(answerKey, answer).histories[answerKey].wrongTypedN++;
                    if (speechInputToggle.On) speechManager.Record();
                    if (wrongChair.histories[answerKey].wrongTypedN == 2) {
                        wrongChair.button.text = $"{answer[0]}";
                    } else if (wrongChair.histories[answerKey].wrongTypedN > 2) {
                        wrongChair.button.text = answer;
                    }
                }// BROKEN for multichoice
                break;
            case TeacherMode.RelatedInfoQuestion:
                string typedAnswer = teacherEar.text;
                if (speechInputToggle.On) {
                    typedAnswer = speechManager.result;
                }
                typedAnswer = typedAnswer.ToLower();
                allRight = typedAnswer.Contains(answerChair.attributes[answerKey].ToLower());
                mainGroup.SelectChair(answerChair);
                answerChair.RevealAnswer(answerKey, allRight);
                if (allRight) { // feels like should be a method
                    teacherMode = TeacherMode.Explaining;
                    answerChair.histories[answerKey].rightRelatedN++;
                    if (answerChair.histories[answerKey].wrongRelatedN < answerChair.histories[answerKey].rightRelatedN) {
                        mainGroup.teaching.Remove(answerChair);
                    }
                } else {
                    answerChair.histories[answerKey].wrongRelatedN++;
                    if (answerChair.histories[answerKey].wrongRelatedN == 2) {
                        answerChair.button.text = $"{answerChair.attributes[answerKey][0]}";
                    } else if (answerChair.histories[answerKey].wrongRelatedN > 2) {
                        answerChair.button.text = answerChair.attributes[answerKey];
                    }
                    if (speechInputToggle.On) speechManager.Record();
                }
                teacherEar.Focus();
                break;
            case TeacherMode.ChairClickQuestion:
                allRight = mainGroup.RightOnMatch(answerKey, answer);
                if (allRight) teacherMode = TeacherMode.Explaining;
                break;
            case TeacherMode.MatchChairQuestion:
                allRight = true;
                foreach (Chair chair in choiceGroup.chairs) {
                    Chair checkChair = mainGroup.GetChairFromPos(chair);
                    if (checkChair == null) {
                        allRight = false;
                        chair.RevealAnswer(answerKey, false);
                        continue;
                    }
                    bool right = checkChair.attributes.ContainsKey(answerKey) && chair.attributes[answerKey] == checkChair.attributes[answerKey];
                    chair.RevealAnswer(answerKey, right);
                    if (right) {
                        checkChair.histories[answerKey].rightMultiChoiceN++;
                    } else {
                        chair.X = chair.X;
                        chair.Y = chair.Y;
                        allRight = false;
                        checkChair.histories[answerKey].wrongMultiChoiceN++;
                        checkChair.RevealAnswer(answerKey, right);
                    }
                }
                if (allRight) {
                    teacherMode = TeacherMode.Explaining;
                }
                break;
            case TeacherMode.Explaining:
                allRight = true;
                WipeWhiteboard();
                int lowestLevel = mainGroup.LowestLevel(answerKey);
                if (lowestLevelOverride.On) lowestLevel = (int)lowestLevelField.value;
                else lowestLevelField.value = (uint)lowestLevel;
              //if (lowestLevel == 3) answerKey = "lastName";
              //lowestLevel = mainGroup.LowestLevel(answerKey);
              //if (lowestLevel == 3) answerKey = "group";
              //lowestLevel = mainGroup.LowestLevel(answerKey);
                if (lowestLevel < 3) {
                    if (episodeIndex == 0) {
                        mainGroup.teaching = new List<Chair>();
                        teacher.text = "Remember the positions of these people";
                        mainGroup.mode = GroupMode.Program;
                        mainGroup.nSelectable = toTeach;
                        mainGroup.SetTeachingChairs(answerKey, teachAscending, lowestLevel, toTeach);
                        if (lowestLevel == 0) {
                            foreach (Chair chair in mainGroup.teaching) {
                                chair.ShowAttribute(answerKey);
                                mainGroup.SelectChair(chair);
                            }
                        } else {
                            episodeIndex++;
                        }
                    }
                    if (episodeIndex == 1) {
                        if (lowestLevel < 2) {
                            AskMatchQuestion(answerKey);
                            //AskChairClickQuestion($"Select the people with the attribute {answerKey}", answerKey, "t");
                        } else {
                            episodeIndex++;
                        }
                    }
                    if (episodeIndex > 1) {
                        if (mainGroup.teaching.Count > 0) {
                            AskAboutChairQuestion("Who sits here?", answerKey, mainGroup.GetRandomTeachingChair().attributes[answerKey], 0);
                        } else {
                            episodeIndex = 7;
                        }
                    }
                } else if (lowestLevel == 3 || subordinate) {
                    if (mainGroup.teaching.Count == 0) {
                        mainGroup.teaching = mainGroup.chairs.GetRange(0, mainGroup.chairs.Count);
                    }
                    AskRandomRelatedInfoQuestion("", answerKey);
                } else {
                    if (episodeIndex == 0 || episodeIndex == 3) {
                        root.visible = false;
                        classroom.root.visible = true;
                    } else {
                        if (mainGroup.teaching.Count == 0) {
                            mainGroup.teaching = mainGroup.chairs.GetRange(0, mainGroup.chairs.Count);
                        }
                        AskRandomRelatedInfoQuestion("", answerKey);
                    }
                }
              //if (episodeIndex == 0) {
              //    answerKey = "director";
              //} else if (episodeIndex == 1) {
              //    answerKey = "story";
              //} else if (episodeIndex == 2) {
              //    answerKey = "screenplay";
              //} else if (episodeIndex == 3) {
              //    answerKey = "songs";
              //} else if (episodeIndex == 4) {
              //    answerKey = "actor";
              //}
              //AskChairClickQuestion($"Select the people with the attribute {answerKey}", answerKey, "t");
                episodeIndex++;
                if (teacherMode != TeacherMode.Explaining) {
                    if (subordinate) {
                        root.visible = false;
                        classroom.root.visible = true;
                    }
                }
                if (episodeIndex > 5) episodeIndex = 0;
                break;
        }
        mainGroup.SaveHistories(chairsFileName);
    }

    void AskAboutChairQuestion(string question, string key, string value, int nChoices) {
        mainGroup.nSelectable = 1;
        mainGroup.mode = GroupMode.Program;
        if (nChoices == 0) {
            teacher.text = question;
            teacherEar.visible = true;
            teacherEar.Focus();
        } else {
            choiceGroup = mainGroup.RandomSelectionAround(key, value, nChoices);
            foreach (Chair choice in choiceGroup.chairs) {
                choice.ShowAttribute(key);
            }
        }
        answer = value;
        answerKey = key;
        teacherMode = TeacherMode.AboutChairQuestion;
        mainGroup.SelectChair(mainGroup.GetChair(key, value));
        if (speechInputToggle.On) speechManager.Record();
    }

    void AskRandomRelatedInfoQuestion(string key1In="", string key2In="") {
        int chairIndex = (int)Random.Range(0,mainGroup.teaching.Count-1);
        Chair chair = mainGroup.teaching[chairIndex];
        bool possible = false;
        string key1 = key1In;
        string key2 = key2In;
        while (!possible) {
            if (key1In == "") {
                int keyIndex = (int)Random.Range(0,chair.attributes.Count-1);
                key1 = chair.attributes.Keys.ElementAt(keyIndex);
            }
            if (key2In == "") {
                int keyIndex = (int)Random.Range(0,chair.attributes.Count-1);
                key2 = chair.attributes.Keys.ElementAt(keyIndex);
            }
            int nMatch = 0;
            foreach (Chair checkChair in mainGroup.chairs) {
                if (checkChair.attributes.ContainsKey(key1) && checkChair.attributes[key1] == chair.attributes[key1]) {
                    nMatch++;
                }
            }
            possible = nMatch == 1 && key1 != key2;
        }
        teacher.text = $"If {key1} is {chair.attributes[key1]} what is their {key2}";
        mainGroup.nSelectable = 1;
        mainGroup.mode = GroupMode.Program;
        teacherEar.visible = true;
        teacherMode = TeacherMode.RelatedInfoQuestion;
        answerKey = key2;
        answerChair = chair;
        teacherEar.Focus();
        if (speechInputToggle.On) speechManager.Record();
    }

    void AskChairClickQuestion(string question, string key, string value) {
        teacher.text = question;
        List<Chair> allAnswers = mainGroup.GetRelatedChairs(key, value);
        mainGroup.nSelectable = allAnswers.Count;
        mainGroup.mode = GroupMode.Click;
        answer = value;
        answerKey = key;
        teacherMode = TeacherMode.ChairClickQuestion;
    }


    void AskMatchQuestion(string key) {
        mainGroup.nSelectable = mainGroup.teaching.Count;
        mainGroup.mode = GroupMode.Program;
        answerKey = key;
        teacherMode = TeacherMode.MatchChairQuestion;
        choiceGroup = mainGroup.SubTeaching();
        choiceGroup.Shuffle();
        foreach (Chair chair in mainGroup.teaching) {
            mainGroup.SelectChair(chair);
        }
        foreach (Chair choice in choiceGroup.chairs) {
            choice.ShowAttribute(key, false);
        }
        choiceGroup.mode = GroupMode.Drag;
        choiceGroup.dragTargets = mainGroup.selected.ToList();
    }

    void AskMatchQuestion(string key, int startIndex, int n) {
        mainGroup.nSelectable = n;
        mainGroup.mode = GroupMode.Program;
        answerKey = key;
        teacherMode = TeacherMode.MatchChairQuestion;
        choiceGroup = mainGroup.SubSelect(startIndex, n);
        choiceGroup.Shuffle();
        for (int i = startIndex; i < startIndex+n; i++) {
            mainGroup.SelectChair(mainGroup.chairs[i]);
        }
        foreach (Chair choice in choiceGroup.chairs) {
            choice.button.text = choice.attributes[key];
        }
        choiceGroup.mode = GroupMode.Drag;
        choiceGroup.dragTargets = mainGroup.selected.ToList();
    }

    void WipeWhiteboard() {
        mainGroup.ClearSelection();
        mainGroup.nSelectable = 0;
        choiceGroup.ClearSelection();
        choiceGroup.DeleteChairs(root);
        teacherEar.value = "";
        teacherEar.visible = false;
        teacher.text = "";
    }

    public void AskQuestion(TeacherMode mode, string question, string key = "", string value = "", int nChoices = 0, int startIndex = 0) {
        switch (mode) {
            case TeacherMode.MatchChairQuestion:
                AskMatchQuestion(key, startIndex, nChoices);
                break;
            case TeacherMode.ChairClickQuestion:
                AskChairClickQuestion(question, key, value);
                break;
            case TeacherMode.AboutChairQuestion:
                AskAboutChairQuestion(question, key, value, nChoices);
                break;
            case TeacherMode.Explaining:
                teacher.text = question;
                teacherMode = mode;
                break;
        }
    }

    void Dropdown() {
        GenericDropdownMenu menu = new GenericDropdownMenu();
        foreach (string attribute in allAttributes) {
            menu.AddItem(attribute, false, value => {DropdownSelect(value);}, attribute);
        }
        menu.DropDown(answerKeyMenuButton.worldBound, answerKeyMenuButton, false);
    }
    void DropdownSelect(object value) {
        string key = (string)value;
        key = key.Trim();
        answerKey = key;
        teacherMode = TeacherMode.Explaining;
        WipeWhiteboard();
        answerKeyMenuButton.text = key;
    }

    void LoadCSV(string filename) {
        // Load CSV file from Resources
        TextAsset file = Resources.Load<TextAsset>(filename);
        if (file == null) {
            Debug.LogError("CSV file not found in Resources!");
            return;
        }

        string[] lines = file.text.Split('\n');

        int x_column = 0;
        int y_column = 0;
        string[] headers = lines[0].Split(',');
        List<Chair> chairs = new List<Chair>();
        allAttributes = new List<string>();
        for (int i = 0; i < headers.Length; i++) {
            if (headers[i] == "x") {
                x_column = i;
            } else if (headers[i] == "y") {
                y_column = i;
            } else {
                allAttributes.Add(headers[i]);
            }
        }
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');

            float x;
            float y;
            if (string.IsNullOrWhiteSpace(lines[i]) ||
                !float.TryParse(values[x_column], out x) ||
                !float.TryParse(values[y_column], out y)
            ) continue;

            Dictionary<string, string> attributes = new Dictionary<string, string>();
            for (int j = 0; j < values.Length; j++) {
                if (
                    !string.IsNullOrWhiteSpace(values[j]) &&
                    j != x_column &&
                    j != y_column
                ) {
                    attributes[headers[j].Trim()] = values[j].Trim();
                }
            }

            Chair chair = new Chair(x, y, attributes, root);
            chairs.Add(chair);
        }

        ChairXComparer chairXComparer = new ChairXComparer();
        chairs.Sort(chairXComparer);
        mainGroup = new SelectableGroup(chairs, 0);
        Debug.Log("Loaded " + chairs.Count + " chairs from CSV!");
        bool loadedHistory = mainGroup.LoadHistories(filename, false);
        if (loadedHistory) Debug.Log($"Loaded history file from: {mainGroup.GetSavePath(filename)}");
    }

    void Update() {
        if (root.visible) {
            if (Input.GetKeyDown(KeyCode.Return)) {
                if (speechInputToggle.On &&
                    (teacherMode == TeacherMode.AboutChairQuestion ||
                     teacherMode == TeacherMode.RelatedInfoQuestion)) {
                    if (!speechManager.StopRecord()) {
                        Accept();
                    }
                } else {
                    Accept();
                }
            }
            if (Input.GetKeyUp(KeyCode.Return)) {
                if (choiceGroup.chairs.Count == 0 && root.focusController.focusedElement != teacherEar) teacherEar.Focus();
            }
        }
//      switch (teacherMode) {
//          case TeacherMode.AboutChairQuestion:
//          case TeacherMode.RelatedInfoQuestion:
//              if (choiceGroup.chairs.Count == 0 && root.focusController.focusedElement != teacherEar) teacherEar.Focus();
//              break;
//      }
    }
}
