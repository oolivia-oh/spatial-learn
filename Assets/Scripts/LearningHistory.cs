using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class LearningHistory {
    public string value;
    public int rightTypedN = 0;
    public int wrongTypedN = 0;
    public int rightMultiChoiceN = 0;
    public int wrongMultiChoiceN = 0;
    public int rightRelatedN = 0;
    public int wrongRelatedN = 0;

    public LearningHistory(string i_value) {
        value = i_value;
    }

    public int Level {
        get {
            if (rightRelatedN - wrongRelatedN >= 3) return 4;
            if (rightTypedN   - wrongTypedN   >= 2) return 3;
            if (rightTypedN                   >= 1) return 2;
            if (rightMultiChoiceN             > 0)  return 1;
                                                    return 0;
        }
    }

    public static LearningHistory operator +(LearningHistory left, LearningHistory right) {
        LearningHistory history = new LearningHistory(left.value);
        history.rightTypedN = left.rightTypedN + right.rightTypedN;
        history.wrongTypedN = left.wrongTypedN + right.wrongTypedN;
        history.rightMultiChoiceN = left.rightMultiChoiceN + right.rightMultiChoiceN;
        history.wrongMultiChoiceN = left.wrongMultiChoiceN + right.wrongMultiChoiceN;
        history.rightRelatedN = left.rightRelatedN + right.rightRelatedN;
        history.wrongRelatedN = left.wrongRelatedN + right.wrongRelatedN;
        return history;
    }
}