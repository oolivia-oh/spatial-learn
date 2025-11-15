using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

public enum GroupMode {
    Drag,
    Program,
    Click
}

public class SelectableGroup {
    public List<Chair> chairs;
    public Queue<Chair> selected;
    public List<Chair> teaching;
    public int nSelectable;
    public bool canClick;
    public GroupMode mode = GroupMode.Click;
    public List<Chair> dragTargets;
    private Chair draggingChair = null;

    public SelectableGroup (List<Chair> i_chairs, int i_nSelectable) {
        selected = new Queue<Chair>();
        teaching = new List<Chair>();
        nSelectable = i_nSelectable;
        chairs = i_chairs;
        foreach (Chair chair in chairs) {
            chair.button.RegisterCallback<PointerDownEvent>((PointerDownEvent evt) => OnPointerDown(chair.attributes, evt), TrickleDown.TrickleDown);
            chair.button.RegisterCallback<PointerMoveEvent>((PointerMoveEvent evt) => OnPointerMove(chair.attributes, evt));
            chair.button.RegisterCallback<PointerUpEvent>((PointerUpEvent evt) => OnPointerUp(chair.attributes, evt));
            setIdAttributes(chair);
        }
    }

    public void setIdAttributes(Chair chair) {
        int prevMatches = 0;
        foreach (string key in chair.attributes.Keys) {
            chair.idAttributes.Add(key);
            int matches = 0;
            foreach (Chair compare in chairs) {
                bool allMatch = true;
                foreach(string id in chair.idAttributes) {
                    if (compare.attributes[id] != chair.attributes[id]) {
                        allMatch = false;
                    }
                }
                if (allMatch) matches++;
            }
            if (matches == 1) {
                break;
            }
            if (matches == prevMatches) {
                chair.idAttributes.Remove(key);
            }
        }
    }

    public void SelectChair(Chair chair) {
        if (nSelectable > 0) {
            chair.button.style.backgroundColor = GlobalConfig.colors.highlighted;
            selected.Enqueue(chair);
            if (selected.Count > nSelectable) {
                selected.Dequeue().button.style.backgroundColor = GlobalConfig.colors.background;
            }
        }
    }

    public bool RightOnMatch(string answerKey, string answer) {
        bool allRight = true;
        List<Chair> answers = GetRelatedChairs(answerKey, answer);
        foreach (Chair chair in answers) {
            if (selected.Contains(chair)) {
                chair.RevealAnswer(answerKey, true);
            } else {
                allRight = false;
            }
        }
        foreach (Chair chair in selected) {
            if (!answers.Contains(chair)) {
                chair.RevealAnswer(answerKey, false);
                allRight = false;
            }
        }
        return allRight;
    }

    public SelectableGroup RandomSelectionAround(string key, string value, int nChoices) {
        List<Chair> choices = new List<Chair>();
        Chair chair = GetChair(key, value);
        int startIndex = chairs.IndexOf(chair);
        // really need a max
        startIndex -= (int)(Random.value * (float)nChoices);
        if (startIndex < 0) startIndex = 0;
        if (startIndex+nChoices >= chairs.Count) startIndex = chairs.Count - nChoices - 1;
        return SubSelect(startIndex, nChoices);
    }

    public SelectableGroup SubTeaching() {
        List<Chair> subChairs = new List<Chair>();
        for (int i = 0; i < teaching.Count; i++) {
            subChairs.Add(new Chair(i, -1, teaching[i].attributes, teaching[i].button.parent));
        }
        SelectableGroup subGroup = new SelectableGroup(subChairs, 1);
        return subGroup;
    }

    public SelectableGroup SubSelect(int startIndex, int n) {
        List<Chair> subChairs = new List<Chair>();
        for (int i = 0; i < n; i++) {
            Chair currentChair = chairs[i+startIndex];
            subChairs.Add(new Chair(i, -1, currentChair.attributes, currentChair.button.parent));
        }
        SelectableGroup subGroup = new SelectableGroup(subChairs, 1);
        return subGroup;
    }

    public void Shuffle() {
        for (int i = chairs.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (chairs[i],   chairs[j])   = (chairs[j],   chairs[i]);
            (chairs[i].X, chairs[j].X) = (chairs[j].X, chairs[i].X);
            (chairs[i].Y, chairs[j].Y) = (chairs[j].Y, chairs[i].Y);
        }
    }

    public void OnPointerDown(Dictionary<string,string> attributes, PointerDownEvent evt) {
        if (mode != GroupMode.Drag) return;
        Chair chair = GetChair(attributes);
        chair.button.BringToFront();
        draggingChair = chair;
        chair.button.CapturePointer(evt.pointerId);
    }

    public void OnPointerMove(Dictionary<string,string> attributes, PointerMoveEvent evt) {
        Chair chair = GetChair(attributes);
        if (draggingChair != chair || mode != GroupMode.Drag) return;

        chair.button.style.left = evt.position.x - chair.button.style.width.value.value/2;
        chair.button.style.top = evt.position.y - chair.button.style.height.value.value/2;
    }

    public void OnPointerUp(Dictionary<string,string> attributes, PointerUpEvent evt) {
        Chair chair = GetChair(attributes);
        switch (mode) {
            case GroupMode.Click:
                SelectChair(chair);
                break;
            case GroupMode.Drag:
                draggingChair = null;
                chair.button.ReleasePointer(evt.pointerId);
                Chair overChair = PositionOverChair(new Vector2(evt.position.x, evt.position.y), dragTargets);
                if (overChair == null) {
                    chair.X = chair.X;
                    chair.Y = chair.Y;
                } else {
                    chair.button.style.left = overChair.button.style.left;
                    chair.button.style.top = overChair.button.style.top;
                }
                break;
        }
    }

    public Chair GetRandomTeachingChair() {
        int index = Random.Range(0, teaching.Count-1);
        return teaching[index];
    }

    public List<Chair> GetRelatedChairs(string key, string value) {
        List<Chair> related = new List<Chair>();
        foreach (Chair chair in chairs) {
            if (chair.attributes.ContainsKey(key) && chair.attributes[key] == value) related.Add(chair);
        }
        return related;
    }

    public int SetTeachingChairs(string key, bool teachAscending, int level, int maxN) {
        int n = 0;
        if (teachAscending) {
            for (int i = 0; i < chairs.Count && n < maxN; i++) {
                if (chairs[i].attributes.ContainsKey(key) &&
                    chairs[i].histories[key].Level <= level) {
                    teaching.Add(chairs[i]);
                    n++;
                }
            }
        } else {
            for (int i = chairs.Count-1; i >= 0 && n < maxN; i--) {
                if (chairs[i].attributes.ContainsKey(key) &&
                    chairs[i].histories[key].Level <= level) {
                    teaching.Add(chairs[i]);
                    n++;
                }
            }
        }
        return n;
    }

    public int LowestLevel(string key) {
        int lowestLevel = 20000;
        foreach (Chair chair in chairs) {
            if (chair.attributes.ContainsKey(key) &&
                chair.histories[key].Level < lowestLevel) {
                lowestLevel = chair.histories[key].Level;
            }
        }
        return lowestLevel;
    }

    private Chair PositionOverChair(Vector2 position, List<Chair> chairs) {
        Chair overChair = null;
        foreach (Chair chair in chairs) {
            IStyle style = chair.button.style;
            if (position.x >= style.left.value.value && position.x <= style.left.value.value + style.width.value.value &&
                position.y >= style.top.value.value  && position.y <= style.top.value.value  + style.height.value.value) {
                overChair = chair;
            }
        }
        return overChair;
    }

    public Chair GetChair(Dictionary<string,string> attributes) {
        Chair foundChair = chairs[0];
        foreach (Chair chair in chairs) {
            if (chair.attributes == attributes) {
                foundChair = chair;
            }
        }
        return foundChair;
    }

    public Chair GetChairFromPos(Chair compareChair) {
        Chair foundChair = null;
        foreach (Chair chair in chairs) {
            if (chair.button.style.left == compareChair.button.style.left &&
                chair.button.style.top  == compareChair.button.style.top) {
                foundChair = chair;
            }
        }
        return foundChair;
    }

    public Chair GetChair(string key, string value="t") {
        Chair foundChair = chairs[0];
        foreach (Chair chair in chairs) {
            if (chair.attributes.ContainsKey(key) && chair.attributes[key] == value) {
                foundChair = chair;
            }
        }
        return foundChair;
    }

    public void HighlightChair(string key, string value="t") {
        Chair chair = GetChair(key, value);
        chair.button.style.backgroundColor = GlobalConfig.colors.highlighted;
    }

    public bool CheckAnswer(string key, string answer, string guess) {
        Chair chair = GetChair(key, answer);
        bool right = guess.ToLower().Contains(answer.ToLower());
        chair.RevealAnswer(key, right);
        return right;
    }

    public void ClearHighlighting() {
        foreach (Chair chair in chairs) {
            chair.button.style.backgroundColor = GlobalConfig.colors.background;
        }
    }

    public void ClearSelection() {
        while (selected.Count > 0) {
            Chair chair = selected.Dequeue();
            chair.button.style.backgroundColor = GlobalConfig.colors.background;
            chair.button.text = "";
        }
    }

    public void DeleteChairs(VisualElement root) {
        for (int i = chairs.Count-1; i >= 0; i--) {
            root.Remove(chairs[i].button);
            chairs.Remove(chairs[i]);
        }
    }

    public string GetSavePath(string filename) {
        return Application.persistentDataPath + "/" + filename + "_learning-histories.json";
    }

    public void SaveHistories(string filename) {
        List<Dictionary<string,LearningHistory>> histories = new List<Dictionary<string,LearningHistory>>();
        foreach (Chair chair in chairs) {
            histories.Add(chair.histories);
        }
        string json = JsonConvert.SerializeObject(histories, Formatting.Indented);
        System.IO.File.WriteAllText(GetSavePath(filename), json);
    }

    public bool LoadHistories(string filename, bool replace) {
        string path = GetSavePath(filename);
        bool loadedFile = System.IO.File.Exists(path);
        if (loadedFile) {
            string json = System.IO.File.ReadAllText(path);
            List<Dictionary<string,LearningHistory>> chairHistories =
                JsonConvert.DeserializeObject<List<Dictionary<string,LearningHistory>>>(json);
            foreach (Chair chair in chairs) {
                foreach (Dictionary<string,LearningHistory> histories in chairHistories) {
                    bool allMatch = true;
                    foreach (string key in chair.idAttributes) {
                        if (!histories.ContainsKey(key) || histories[key].value != chair.attributes[key]) {
                            allMatch = false;
                        }
                    }
                    if (allMatch) {
                        foreach (string key in histories.Keys) {
                            if (chair.attributes.ContainsKey(key)) {
                                if (replace) {
                                    chair.histories[key] = histories[key];
                                } else {
                                    chair.histories[key] = chair.histories[key] + histories[key];
                                }
                            }
                        }
                    }
                }
            }
        }
        return loadedFile;
    }
}
