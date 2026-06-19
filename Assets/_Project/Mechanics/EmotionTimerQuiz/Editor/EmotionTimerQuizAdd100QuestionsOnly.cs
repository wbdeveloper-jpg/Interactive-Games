#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EmotionTimerQuiz;

public static class EmotionTimerQuizAdd100QuestionsOnly
{
    private struct QuestionSeed
    {
        public string id;
        public string text;
        public CharacterType character;
        public ExpressionType expression;
        public int seconds;

        public QuestionSeed(string id, string text, CharacterType character, ExpressionType expression, int seconds)
        {
            this.id = id;
            this.text = text;
            this.character = character;
            this.expression = expression;
            this.seconds = seconds;
        }
    }

    [MenuItem("Tools/Emotion Timer Quiz/Questions/Add 100 Extra Questions To Selected Set")]
    public static void AddExtraQuestionsToSelectedSet()
    {
        EmotionTimerQuizQuestionSet questionSet = Selection.activeObject as EmotionTimerQuizQuestionSet;

        if (questionSet == null)
        {
            EditorUtility.DisplayDialog(
                "Question Set Not Selected",
                "Select your EmotionTimerQuizQuestionSet asset in the Project window first, then run this menu again.",
                "OK");
            return;
        }

        if (questionSet.questions == null)
        {
            questionSet.questions = new List<SituationQuestion>();
        }

        Undo.RecordObject(questionSet, "Add 100 Emotion Timer Quiz Questions");

        HashSet<string> existingIds = new HashSet<string>();
        for (int i = 0; i < questionSet.questions.Count; i++)
        {
            SituationQuestion existingQuestion = questionSet.questions[i];
            if (existingQuestion != null && !string.IsNullOrEmpty(existingQuestion.id))
            {
                existingIds.Add(existingQuestion.id);
            }
        }

        int addedCount = 0;
        QuestionSeed[] extraQuestions = GetExtraQuestions();

        for (int i = 0; i < extraQuestions.Length; i++)
        {
            QuestionSeed seed = extraQuestions[i];
            if (existingIds.Contains(seed.id))
            {
                continue;
            }

            questionSet.questions.Add(new SituationQuestion
            {
                id = seed.id,
                situationText = seed.text,
                targetCharacter = seed.character,
                correctExpression = seed.expression,
                timeLimitSeconds = seed.seconds
            });

            existingIds.Add(seed.id);
            addedCount++;
        }

        EditorUtility.SetDirty(questionSet);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Questions Updated",
            "Added " + addedCount + " new questions. Total questions now: " + questionSet.questions.Count + ".",
            "OK");
    }

    private static QuestionSeed[] GetExtraQuestions()
    {
        return new QuestionSeed[]
        {
            new QuestionSeed("Q026", "Tina finds a shiny sticker inside her notebook.", CharacterType.TINA, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q027", "Raj loses his favorite pencil during class.", CharacterType.RAJ, ExpressionType.SAD, 15),
            new QuestionSeed("Q028", "Tanvi sees a dog barking loudly near the school gate.", CharacterType.TANVI, ExpressionType.SCARED, 12),
            new QuestionSeed("Q029", "Rajes finishes a hard spelling test by himself.", CharacterType.RAJES, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q030", "Raj hears that his team won the house points prize.", CharacterType.RAJ, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q031", "Tina sees someone scribble on her drawing without asking.", CharacterType.TINA, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q032", "Tanvi shares her crayons with a new student.", CharacterType.TANVI, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q033", "Rajes cannot find his lunch box after the bell rings.", CharacterType.RAJES, ExpressionType.SAD, 15),
            new QuestionSeed("Q034", "Raj walks into a dark room during a power cut.", CharacterType.RAJ, ExpressionType.SCARED, 12),
            new QuestionSeed("Q035", "Tina reads a poem clearly in front of the class.", CharacterType.TINA, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q036", "Tanvi gets chosen to lead the morning activity.", CharacterType.TANVI, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q037", "Rajes sees his toy car broken by someone else.", CharacterType.RAJES, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q038", "Raj gets a kind note from his best friend.", CharacterType.RAJ, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q039", "Tina misses the school bus in the morning.", CharacterType.TINA, ExpressionType.SAD, 15),
            new QuestionSeed("Q040", "Tanvi hears a loud fire alarm during practice.", CharacterType.TANVI, ExpressionType.SCARED, 12),
            new QuestionSeed("Q041", "Rajes answers the teacher's question correctly.", CharacterType.RAJES, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q042", "Raj sees balloons and games ready for the class party.", CharacterType.RAJ, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q043", "Tina waits a long time but someone cuts the line.", CharacterType.TINA, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q044", "Tanvi gets praised for keeping her desk neat.", CharacterType.TANVI, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q045", "Rajes drops his craft model and it falls apart.", CharacterType.RAJES, ExpressionType.SAD, 15),
            new QuestionSeed("Q046", "Raj sees a big lizard on the classroom wall.", CharacterType.RAJ, ExpressionType.SCARED, 12),
            new QuestionSeed("Q047", "Tina ties her shoelaces without help for the first time.", CharacterType.TINA, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q048", "Tanvi learns that tomorrow is sports day.", CharacterType.TANVI, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q049", "Rajes sees someone hide his school bag as a joke.", CharacterType.RAJES, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q050", "Raj helps his friend pick up fallen books.", CharacterType.RAJ, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q051", "Tina breaks her favorite hair clip by mistake.", CharacterType.TINA, ExpressionType.SAD, 15),
            new QuestionSeed("Q052", "Tanvi hears thunder while standing near a window.", CharacterType.TANVI, ExpressionType.SCARED, 12),
            new QuestionSeed("Q053", "Rajes completes his handwriting page neatly.", CharacterType.RAJES, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q054", "Raj gets picked for the school dance group.", CharacterType.RAJ, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q055", "Tina sees her classmate tear a page from her book.", CharacterType.TINA, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q056", "Tanvi receives a smiley stamp from her teacher.", CharacterType.TANVI, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q057", "Rajes forgets his lines during a small play.", CharacterType.RAJES, ExpressionType.SAD, 15),
            new QuestionSeed("Q058", "Raj sees a stranger shouting near the playground.", CharacterType.RAJ, ExpressionType.SCARED, 12),
            new QuestionSeed("Q059", "Tina shows her project confidently to the class.", CharacterType.TINA, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q060", "Tanvi finds out her cousins are visiting tonight.", CharacterType.TANVI, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q061", "Rajes sees someone splash water on his clean uniform.", CharacterType.RAJES, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q062", "Raj gets a new storybook from the library.", CharacterType.RAJ, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q063", "Tina's paper plane falls into a puddle.", CharacterType.TINA, ExpressionType.SAD, 15),
            new QuestionSeed("Q064", "Tanvi gets lost for a moment in a big mall.", CharacterType.TANVI, ExpressionType.SCARED, 12),
            new QuestionSeed("Q065", "Rajes raises his hand and gives the right answer.", CharacterType.RAJES, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q066", "Raj is told he can plant a seed in the school garden.", CharacterType.RAJ, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q067", "Tina sees her game turn taken by someone else.", CharacterType.TINA, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q068", "Tanvi makes her grandmother laugh with a funny story.", CharacterType.TANVI, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q069", "Rajes watches his sandcastle get washed away.", CharacterType.RAJES, ExpressionType.SAD, 15),
            new QuestionSeed("Q070", "Raj hears a sudden loud noise behind him.", CharacterType.RAJ, ExpressionType.SCARED, 12),
            new QuestionSeed("Q071", "Tina solves a puzzle after trying many times.", CharacterType.TINA, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q072", "Tanvi sees a magic show starting on stage.", CharacterType.TANVI, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q073", "Rajes sees someone push ahead during lunch time.", CharacterType.RAJES, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q074", "Raj gives water to a thirsty puppy.", CharacterType.RAJ, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q075", "Tina's team loses the relay race.", CharacterType.TINA, ExpressionType.SAD, 15),
            new QuestionSeed("Q076", "Tanvi has to walk past a very dark corridor.", CharacterType.TANVI, ExpressionType.SCARED, 12),
            new QuestionSeed("Q077", "Rajes presents his drawing without feeling shy.", CharacterType.RAJES, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q078", "Raj hears that his birthday cake is ready.", CharacterType.RAJ, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q079", "Tina sees mud thrown on her clean shoes.", CharacterType.TINA, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q080", "Tanvi gets a warm hug from her mother after school.", CharacterType.TANVI, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q081", "Rajes cannot go outside because it is raining heavily.", CharacterType.RAJES, ExpressionType.SAD, 15),
            new QuestionSeed("Q082", "Raj sees a bee flying close to his face.", CharacterType.RAJ, ExpressionType.SCARED, 12),
            new QuestionSeed("Q083", "Tina counts money correctly at the pretend shop.", CharacterType.TINA, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q084", "Tanvi is invited to perform a song at assembly.", CharacterType.TANVI, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q085", "Rajes sees his friend being teased unfairly.", CharacterType.RAJES, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q086", "Raj finishes cleaning his room and feels proud.", CharacterType.RAJ, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q087", "Tina forgets to bring her favorite lunch snack.", CharacterType.TINA, ExpressionType.SAD, 15),
            new QuestionSeed("Q088", "Tanvi hears a dog growling near the gate.", CharacterType.TANVI, ExpressionType.SCARED, 12),
            new QuestionSeed("Q089", "Rajes learns to ride his bicycle without support.", CharacterType.RAJES, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q090", "Raj sees the school bus arriving for a picnic.", CharacterType.RAJ, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q091", "Tina sees someone take credit for her idea.", CharacterType.TINA, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q092", "Tanvi gets a thank you card from her friend.", CharacterType.TANVI, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q093", "Rajes loses a sticker he saved for many days.", CharacterType.RAJES, ExpressionType.SAD, 15),
            new QuestionSeed("Q094", "Raj sees a shadow moving outside his window at night.", CharacterType.RAJ, ExpressionType.SCARED, 12),
            new QuestionSeed("Q095", "Tina stands tall and says sorry after a mistake.", CharacterType.TINA, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q096", "Tanvi opens a box and finds colorful craft supplies.", CharacterType.TANVI, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q097", "Rajes sees his seat taken even though he kept his bag there.", CharacterType.RAJES, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q098", "Raj gets a star for helping clean the classroom.", CharacterType.RAJ, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q099", "Tina drops her clay model before showing it to the teacher.", CharacterType.TINA, ExpressionType.SAD, 15),
            new QuestionSeed("Q100", "Tanvi hears a balloon burst very close to her.", CharacterType.TANVI, ExpressionType.SCARED, 12),
            new QuestionSeed("Q101", "Rajes completes a difficult maze without help.", CharacterType.RAJES, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q102", "Raj hears that his cousin is bringing a new board game.", CharacterType.RAJ, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q103", "Tina sees her friend laughing after she falls down.", CharacterType.TINA, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q104", "Tanvi feeds birds with her father in the park.", CharacterType.TANVI, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q105", "Rajes misses his friend who moved to another city.", CharacterType.RAJES, ExpressionType.SAD, 15),
            new QuestionSeed("Q106", "Raj has to visit the dentist for the first time.", CharacterType.RAJ, ExpressionType.SCARED, 12),
            new QuestionSeed("Q107", "Tina finishes her homework before dinner by herself.", CharacterType.TINA, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q108", "Tanvi hears music start for her favorite dance.", CharacterType.TANVI, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q109", "Rajes sees his painting smudged by another child.", CharacterType.RAJES, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q110", "Raj gets to sit next to his best friend on the bus.", CharacterType.RAJ, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q111", "Tina's kite string breaks and the kite flies away.", CharacterType.TINA, ExpressionType.SAD, 15),
            new QuestionSeed("Q112", "Tanvi sees a big wave coming near her sand toys.", CharacterType.TANVI, ExpressionType.SCARED, 12),
            new QuestionSeed("Q113", "Rajes gives a clear answer during quiz practice.", CharacterType.RAJES, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q114", "Raj sees a surprise decoration in his classroom.", CharacterType.RAJ, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q115", "Tina sees someone throw trash near her plants.", CharacterType.TINA, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q116", "Tanvi receives a new ribbon for her hair.", CharacterType.TANVI, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q117", "Rajes cannot play because his football is flat.", CharacterType.RAJES, ExpressionType.SAD, 15),
            new QuestionSeed("Q118", "Raj hears a loud knock when he is alone in a room.", CharacterType.RAJ, ExpressionType.SCARED, 12),
            new QuestionSeed("Q119", "Tina speaks politely to a guest without feeling shy.", CharacterType.TINA, ExpressionType.CONFIDENT, 12),
            new QuestionSeed("Q120", "Tanvi hears that the class will watch a cartoon movie.", CharacterType.TANVI, ExpressionType.EXCITED, 10),
            new QuestionSeed("Q121", "Rajes sees someone break the class rules again and again.", CharacterType.RAJES, ExpressionType.ANGRY, 15),
            new QuestionSeed("Q122", "Raj gets a high five from his coach after practice.", CharacterType.RAJ, ExpressionType.HAPPY, 15),
            new QuestionSeed("Q123", "Tina cannot find her birthday card for her friend.", CharacterType.TINA, ExpressionType.SAD, 15),
            new QuestionSeed("Q124", "Tanvi sees a spider crawling on her school bag.", CharacterType.TANVI, ExpressionType.SCARED, 12),
            new QuestionSeed("Q125", "Rajes leads his group and explains the poster clearly.", CharacterType.RAJES, ExpressionType.CONFIDENT, 12)
        };
    }
}
#endif
