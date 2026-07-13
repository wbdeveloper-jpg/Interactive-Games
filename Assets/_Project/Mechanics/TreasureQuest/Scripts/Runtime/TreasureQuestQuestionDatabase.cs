using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TreasureQuestQuestion
{
    public string subject = "English";
    [Range(1, 5)] public int gateNumber = 1;
    public string difficulty = "Easy";
    [TextArea(1, 3)] public string questionText;
    [TextArea(1, 2)] public string[] options = new string[4];
    [Range(0, 3)] public int correctOptionIndex;
}

public class TreasureQuestQuestionDatabase : MonoBehaviour
{
    [Header("Default Data")]
    public bool useBuiltInEnglishQuestions = true;
    public string defaultSubject = "English";

    [Header("Designer Added Questions")]
    public List<TreasureQuestQuestion> customQuestions = new List<TreasureQuestQuestion>();

    public List<TreasureQuestQuestion> GetQuestions(string subject, int gateNumber)
    {
        var allQuestions = new List<TreasureQuestQuestion>();

        if (useBuiltInEnglishQuestions)
            allQuestions.AddRange(CreateDefaultEnglishQuestions());

        if (customQuestions != null && customQuestions.Count > 0)
            allQuestions.AddRange(customQuestions);

        var result = new List<TreasureQuestQuestion>();
        string safeSubject = string.IsNullOrWhiteSpace(subject) ? defaultSubject : subject;

        for (int i = 0; i < allQuestions.Count; i++)
        {
            TreasureQuestQuestion q = allQuestions[i];
            if (q == null) continue;

            bool subjectMatch = string.Equals(q.subject, safeSubject, StringComparison.OrdinalIgnoreCase);
            if (subjectMatch && q.gateNumber == gateNumber)
                result.Add(q);
        }

        return result;
    }

    public static List<TreasureQuestQuestion> CreateDefaultEnglishQuestions()
    {
        var list = new List<TreasureQuestQuestion>(100);

        // Gate 1: easy and very common phrases.
        Add(list, 1, "Easy", "Safe and sound", "completely safe and uninjured", "lost in a noisy place", "moving very slowly today", "feeling angry at someone");
        Add(list, 1, "Easy", "Pros and cons", "good points and bad points", "people who play sports", "rules for a classroom", "coins kept in pockets");
        Add(list, 1, "Easy", "Rain or shine", "whatever the weather is", "only during heavy rain", "when the sun is bright", "a game played outside");
        Add(list, 1, "Easy", "Ups and downs", "good times and bad times", "stairs inside a house", "jumping over small stones", "moving toys on shelves");
        Add(list, 1, "Easy", "Piece of cake", "something very easy to do", "a sweet food after dinner", "a task that takes years", "a small birthday candle");
        Add(list, 1, "Easy", "Once in a while", "sometimes but not often", "every minute of the day", "only at lunch time", "never at any time");
        Add(list, 1, "Easy", "In a hurry", "doing something very quickly", "sleeping for a long time", "walking with no shoes", "reading very quietly");
        Add(list, 1, "Easy", "Better late than never", "late is better than not doing", "never try again later", "always arrive after school", "finish before everyone else");
        Add(list, 1, "Easy", "Day and night", "all the time", "only in the morning", "when the room is dark", "a school holiday name");
        Add(list, 1, "Easy", "Give up", "stop trying to do something", "give a gift to someone", "stand up very quickly", "open a closed window");
        Add(list, 1, "Easy", "Try your best", "make your strongest effort", "copy another student's answer", "hide from a hard task", "finish without reading first");
        Add(list, 1, "Easy", "All ears", "listening very carefully", "having very big ears", "covering your ears tightly", "wearing new headphones");
        Add(list, 1, "Easy", "Good as gold", "behaving very well", "made from real gold", "very heavy to carry", "shiny like the sun");
        Add(list, 1, "Easy", "Side by side", "next to each other", "far away from everyone", "one above another", "hidden behind a door");
        Add(list, 1, "Easy", "On time", "at the correct time", "after everyone has left", "before learning the rules", "during a rainy day");
        Add(list, 1, "Easy", "Take care", "be careful and safe", "take someone's pencil away", "carry a large box", "start running very fast");
        Add(list, 1, "Easy", "Look after", "take care of someone", "look behind a chair", "search for a lost coin", "watch a funny movie");
        Add(list, 1, "Easy", "Have fun", "enjoy what you are doing", "finish very difficult homework", "feel worried about a test", "stand still without talking");
        Add(list, 1, "Easy", "Calm down", "become less upset or excited", "fall down on the floor", "speak louder than before", "run faster in a race");
        Add(list, 1, "Easy", "Big deal", "something important", "a very large meal", "a giant playing card", "a bag full of toys");

        // Gate 2: easy-medium phrases.
        Add(list, 2, "Easy-Medium", "Break the ice", "make people feel comfortable", "break frozen water apart", "win a race on ice", "hide from new friends");
        Add(list, 2, "Easy-Medium", "Hold on", "wait for a short time", "drop everything quickly", "speak without stopping", "paint a wall blue");
        Add(list, 2, "Easy-Medium", "Find out", "learn new information", "lose something important", "go outside to play", "build a tall tower");
        Add(list, 2, "Easy-Medium", "Keep an eye on", "watch something carefully", "close your eyes tightly", "draw a big eye", "forget where it is");
        Add(list, 2, "Easy-Medium", "Let the cat out of the bag", "tell a secret by mistake", "feed a pet after school", "carry a bag of books", "open a door for animals");
        Add(list, 2, "Easy-Medium", "Under the weather", "feeling a little sick", "standing below a cloud", "watching rain from inside", "wearing a warm jacket");
        Add(list, 2, "Easy-Medium", "Hit the books", "study hard", "throw books on a table", "clean the classroom shelf", "write your name slowly");
        Add(list, 2, "Easy-Medium", "On cloud nine", "very happy", "high in an airplane", "looking at the sky", "sleeping on a soft bed");
        Add(list, 2, "Easy-Medium", "In hot water", "in trouble", "taking a warm bath", "making tea for family", "washing plates after dinner");
        Add(list, 2, "Easy-Medium", "Miss the boat", "lose a chance", "forget a toy boat", "travel across a river", "wave at a ship");
        Add(list, 2, "Easy-Medium", "Step by step", "one part at a time", "jumping over every stair", "running without stopping", "finishing before starting");
        Add(list, 2, "Easy-Medium", "At the last minute", "just before time ends", "when the day begins", "after many weeks pass", "during the first lesson");
        Add(list, 2, "Easy-Medium", "Out of the blue", "suddenly and unexpectedly", "painted blue all over", "outside during the evening", "taken from a blue bag");
        Add(list, 2, "Easy-Medium", "Back to square one", "start again from the beginning", "draw a square again", "sit in the first row", "return to the playground");
        Add(list, 2, "Easy-Medium", "In the same boat", "in the same situation", "sailing with classmates", "sitting near the window", "sharing the same pencil");
        Add(list, 2, "Easy-Medium", "Think twice", "consider carefully before doing", "count two numbers again", "answer without reading", "choose the first option");
        Add(list, 2, "Easy-Medium", "Make up your mind", "decide what to do", "draw a face in art", "forget your homework", "change your hair style");
        Add(list, 2, "Easy-Medium", "No problem", "it is not difficult", "there is a big mistake", "the answer is hidden", "the game is finished");
        Add(list, 2, "Easy-Medium", "In a nutshell", "in a few words", "inside a small nut", "a snack after lunch", "a story with animals");
        Add(list, 2, "Easy-Medium", "Team up", "work together", "stand alone quietly", "pick a new color", "finish before the team");

        // Gate 3: medium phrases.
        Add(list, 3, "Medium", "Beat around the bush", "avoid saying something directly", "run around a small plant", "clean leaves from a garden", "win a game outside");
        Add(list, 3, "Medium", "Bite your tongue", "stop yourself from speaking", "eat food too quickly", "say everything loudly", "drink very cold water");
        Add(list, 3, "Medium", "Cost an arm and a leg", "cost a lot of money", "need strong hands and feet", "hurt after playing sports", "buy a new school bag");
        Add(list, 3, "Medium", "Get the ball rolling", "start an activity", "play football after class", "drop a ball downhill", "stop a game early");
        Add(list, 3, "Medium", "Go the extra mile", "make more effort than expected", "walk one more road", "race without shoes", "travel to another city");
        Add(list, 3, "Medium", "Hang in there", "keep trying during difficulty", "hang a picture on a wall", "stay inside a cupboard", "wait on a tree branch");
        Add(list, 3, "Medium", "Hit the nail on the head", "say exactly the right thing", "fix wood with a hammer", "hurt your head by mistake", "make a loud classroom sound");
        Add(list, 3, "Medium", "Learn the ropes", "learn how something works", "tie knots in a rope", "climb using a rope", "skip during sports class");
        Add(list, 3, "Medium", "Pull yourself together", "control your feelings and act", "pull a heavy object", "stand close to friends", "collect scattered papers");
        Add(list, 3, "Medium", "See eye to eye", "agree with someone", "look closely at eyes", "wear the same glasses", "stand at the same height");
        Add(list, 3, "Medium", "Speak of the devil", "someone mentioned appears", "talk about scary stories", "speak very loudly", "say something unkind");
        Add(list, 3, "Medium", "The best of both worlds", "two good things together", "the best map in class", "two planets in space", "choosing only one side");
        Add(list, 3, "Medium", "Time flies", "time passes very quickly", "a clock can fly", "flies sit on food", "a bird watches time");
        Add(list, 3, "Medium", "Turn over a new leaf", "make a fresh good start", "flip a leaf in a book", "plant a new tree", "paint leaves green");
        Add(list, 3, "Medium", "A blessing in disguise", "a hidden good result", "a costume for a show", "a wish before a meal", "a gift wrapped badly");
        Add(list, 3, "Medium", "Actions speak louder than words", "what you do matters more", "speak in a very loud voice", "write words in big letters", "move without saying anything");
        Add(list, 3, "Medium", "Add fuel to the fire", "make a problem worse", "cook food on a stove", "light a campfire safely", "help someone feel better");
        Add(list, 3, "Medium", "Call it a day", "stop working for now", "name a day of week", "call a friend today", "start a new lesson");
        Add(list, 3, "Medium", "Cross your fingers", "hope for good luck", "make an angry hand sign", "count your fingers again", "hide your hands quickly");
        Add(list, 3, "Medium", "Every cloud has a silver lining", "bad things can have good sides", "clouds are made of silver", "rain always comes soon", "the sky is always dark");

        // Gate 4: medium-hard phrases.
        Add(list, 4, "Medium-Hard", "Burn the midnight oil", "study or work late", "spill oil at night", "light a lamp outside", "cook dinner very slowly");
        Add(list, 4, "Medium-Hard", "Cut corners", "do something too quickly or cheaply", "cut paper into squares", "turn around a corner", "decorate the classroom wall");
        Add(list, 4, "Medium-Hard", "Face the music", "accept the result of actions", "look at a music player", "sing in front of class", "dance to a loud song");
        Add(list, 4, "Medium-Hard", "Jump to conclusions", "decide without enough facts", "jump after finishing work", "read the last page first", "win a long jumping game");
        Add(list, 4, "Medium-Hard", "Keep your chin up", "stay hopeful and brave", "look at the ceiling", "hold your face still", "walk with your head high");
        Add(list, 4, "Medium-Hard", "Make ends meet", "manage with available money", "tie two ropes together", "finish both ends first", "meet friends after school");
        Add(list, 4, "Medium-Hard", "Pull someone's leg", "joke with someone", "help someone stand up", "pull during a race", "make someone walk faster");
        Add(list, 4, "Medium-Hard", "Put two and two together", "understand from clues", "add numbers in math", "put pairs in boxes", "stand in two lines");
        Add(list, 4, "Medium-Hard", "The ball is in your court", "it is your turn to act", "a ball is on the ground", "play tennis after lunch", "the court is empty");
        Add(list, 4, "Medium-Hard", "Through thick and thin", "in good and bad times", "walk through a forest", "wear thick and thin clothes", "read books of two sizes");
        Add(list, 4, "Medium-Hard", "Throw in the towel", "stop trying or quit", "clean water from the floor", "throw laundry into a basket", "start a sports match");
        Add(list, 4, "Medium-Hard", "Under your nose", "very close but unnoticed", "below your face", "smelling something sweet", "hiding inside a nose");
        Add(list, 4, "Medium-Hard", "Walking on eggshells", "acting very carefully", "walking on breakfast food", "making eggs for dinner", "cleaning the kitchen floor");
        Add(list, 4, "Medium-Hard", "When pigs fly", "something that will not happen", "animals flying in a story", "a farm game for children", "a windy day outside");
        Add(list, 4, "Medium-Hard", "You can say that again", "I strongly agree", "please repeat every word", "say it only once", "write the answer again");
        Add(list, 4, "Medium-Hard", "A close call", "almost a bad result", "a phone call nearby", "calling a friend softly", "standing close to a wall");
        Add(list, 4, "Medium-Hard", "Against the clock", "racing to finish in time", "standing against a clock", "fixing a broken watch", "counting every hour");
        Add(list, 4, "Medium-Hard", "In the nick of time", "just before it is too late", "after the chance is gone", "during a school break", "at the start of class");
        Add(list, 4, "Medium-Hard", "It takes two to tango", "both people share responsibility", "two people must dance", "one person wins alone", "music makes people happy");
        Add(list, 4, "Medium-Hard", "Read between the lines", "understand the hidden meaning", "read only blank spaces", "skip every second line", "draw lines in a book");

        // Gate 5: harder, still school appropriate.
        Add(list, 5, "Hard", "Bite off more than you can chew", "try to do too much", "eat a very large snack", "finish homework too early", "choose the easiest job");
        Add(list, 5, "Hard", "Don't count your chickens before they hatch", "do not celebrate too early", "count animals on a farm", "keep eggs in a basket", "wait for breakfast to cook");
        Add(list, 5, "Hard", "Don't judge a book by its cover", "do not decide by looks", "choose books by pictures", "cover a book neatly", "read only the first page");
        Add(list, 5, "Hard", "Let sleeping dogs lie", "avoid restarting old problems", "wake pets for a walk", "let dogs sleep anywhere", "tell a dog to lie down");
        Add(list, 5, "Hard", "Make a mountain out of a molehill", "make a small problem huge", "build a mountain model", "dig holes in a garden", "climb a hill slowly");
        Add(list, 5, "Hard", "Once in a blue moon", "very rarely", "every night after dinner", "when the moon is blue", "during a rainy morning");
        Add(list, 5, "Hard", "The tip of the iceberg", "a small visible part", "ice floating in water", "a cold mountain top", "a hidden treasure map");
        Add(list, 5, "Hard", "A storm in a teacup", "big fuss over little thing", "rain falling into tea", "a cup breaking suddenly", "making tea during rain");
        Add(list, 5, "Hard", "Barking up the wrong tree", "following the wrong idea", "a dog near a tree", "climbing the wrong branch", "planting a tree badly");
        Add(list, 5, "Hard", "Between a rock and a hard place", "stuck between two hard choices", "standing near large stones", "playing on a rocky path", "hiding behind a wall");
        Add(list, 5, "Hard", "Break the mold", "do something in a new way", "break a clay shape", "copy everyone exactly", "clean a dirty corner");
        Add(list, 5, "Hard", "Burn bridges", "damage a relationship", "set bridges on fire", "walk across a bridge", "draw bridges in art");
        Add(list, 5, "Hard", "By the skin of your teeth", "just barely succeed", "brush your teeth carefully", "smile with clean teeth", "lose because of a mistake");
        Add(list, 5, "Hard", "Get your act together", "start behaving properly", "prepare for a school play", "collect costumes for drama", "act funny for friends");
        Add(list, 5, "Hard", "Go back to the drawing board", "try a new plan again", "draw a picture again", "erase the classroom board", "go back to art class");
        Add(list, 5, "Hard", "Have a lot on your plate", "have many things to do", "eat a very full meal", "carry a heavy plate", "wash dishes after lunch");
        Add(list, 5, "Hard", "In the long run", "over a long time", "running a very long race", "after the school bell", "during a short break");
        Add(list, 5, "Hard", "Leave no stone unturned", "try every possible way", "clean all stones outside", "turn stones for insects", "leave quickly without looking");
        Add(list, 5, "Hard", "Put your best foot forward", "try to make a good impression", "walk with one foot first", "wear your best shoes", "stand in front of line");
        Add(list, 5, "Hard", "The last straw", "the final small problem", "a straw in a drink", "the last farm plant", "a stick for making art");

        return list;
    }

    private static void Add(List<TreasureQuestQuestion> list, int gate, string difficulty, string question, string correct, string wrong1, string wrong2, string wrong3)
    {
        list.Add(new TreasureQuestQuestion
        {
            subject = "English",
            gateNumber = gate,
            difficulty = difficulty,
            questionText = question,
            options = new[] { correct, wrong1, wrong2, wrong3 },
            correctOptionIndex = 0
        });
    }
}
