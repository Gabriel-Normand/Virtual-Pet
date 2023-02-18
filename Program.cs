/*
 * Gabriel Normand
 * Feb 17 2023
 *
 * This is a command line interface application with a virtual pet that you need to take care of.
 *
 * For this application I decided to learn about threading to allow the stats to drain in the background while also
 * waiting for a command.
 */

using System.Diagnostics;

namespace VirtualPet
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            new VirtualPet();
        }
    }

    internal class LineNumber
    {
        public const int STATS = 0;
        public const int INSTRUCTIONS = 1;
        public const int FEEDBACK = 2;
        public const int ACTION = 3;
        public const int CURSOR = 4;
        public const int FINALMESSAGE = 6;
    }

    internal class CauseOfDeath
    {
        public const int AGE = 0;
        public const int HUNGER = 1;
        public const int HEALTH = 2;
        public const int HAPPINESS = 3;
    }

    internal class CauseOfSleep
    {
        public const int ENERGY = 0;
        public const int FOOD = 1;
    }

    public class VirtualPet
    {
        private static readonly object locker = new object();
        private const int INITIAL_DELAY = 1000;
        private bool acting = false;
        private bool exit = false;

        public VirtualPet()
        {
            isAlive = true;
            age = 0;
            food = FOOD_MAX;
            energy = ENERGY_MAX;
            health = HEALTH_MAX;
            happiness = HAPPINESS_MAX / 2;
            DisplayStats();
            DisplayInstructions();
            Thread lifeCycle = new Thread(LifeCycle);
            lifeCycle.Start();

            #region Menu

            ConsoleKeyInfo input = new ConsoleKeyInfo();
            while (input.Key != ConsoleKey.Escape)
            {
                while (!acting && input.Key != ConsoleKey.Escape)
                {
                    while (Console.KeyAvailable)    // KeyAvailable checks for buffered keystrokes
                        Console.ReadKey(true);      // Send all the buffered keystrokes to void
                    input = Console.ReadKey(true);
                    switch (input.Key)
                    {
                        case ConsoleKey.F:
                            Feed();
                            break;

                        case ConsoleKey.B:
                            PutToBed();
                            break;

                        case ConsoleKey.W:
                            Wake();
                            break;

                        case ConsoleKey.C:
                            Clean();
                            break;

                        case ConsoleKey.P:
                            Play();
                            break;
                    }
                }
            }

            #endregion Menu

            DisplayFinalMessage("Goodbye! Thanks for playing!");
            exit = true;
            Thread.Sleep(INITIAL_DELAY);
        }

        #region Life and Death

        private int age;
        private const int AGE_MAX = 100;
        private const int AGE_INTERVAL = (5 * 60 * 1000) / AGE_MAX;
        private const int TODDLER_AGE = 3, TEENAGER_AGE = 13, ADULT_AGE = 18, MIDDLE_AGE = 50, ELDER_AGE = 75;
        private bool isAlive;

        private void LifeCycle()
        {
            Thread.Sleep(INITIAL_DELAY);
            wakeTimer.Start();
            poopTimer.Start();
            Thread[] lifeThreads = {
                new Thread(Age),
                new Thread(HungerDrain),
                new Thread(EnergyDrain),
                new Thread(PoopMachine),
                new Thread(HappinessDrain)};
            foreach (Thread thread in lifeThreads)
            {
                thread.Start();
            }
        }

        // Part of the life cycle thread
        private void Age()
        {
            while (isAlive && !exit)
            {
                age++;
                if (age >= AGE_MAX)
                {
                    age = AGE_MAX;
                    Die(CauseOfDeath.AGE);
                }
                else
                {
                    switch (age)
                    {
                        case TODDLER_AGE:
                            DisplayFeedback("Your pet is now a toddler! You have a long way to go.");
                            break;

                        case TEENAGER_AGE:
                            DisplayFeedback("Your pet is now a teenager! They're so rebellious.");
                            break;

                        case ADULT_AGE:
                            DisplayFeedback("Your pet is now a adult! They grow up so fast.");
                            break;

                        case MIDDLE_AGE:
                            DisplayFeedback("Your pet is now middle-aged! You're doing great, keep it up!");
                            break;

                        case ELDER_AGE:
                            DisplayFeedback("Your pet is now an elder! You're almost there!");
                            break;
                    }
                }
                DisplayStats();
                Thread.Sleep(AGE_INTERVAL);
            }
        }

        private void Die(int causeOfDeath)
        {
            isAlive = false;
            switch (causeOfDeath)
            {
                case CauseOfDeath.AGE:
                    DisplayFinalMessage("Your pet has died of old age! Congratulations!");
                    break;

                case CauseOfDeath.HUNGER:
                    DisplayFinalMessage("Your pet has died of hunger! It's just skin and bones!");
                    break;

                case CauseOfDeath.HEALTH:
                    DisplayFinalMessage("Your pet has died of bad hygiene! Gross!");
                    break;

                case CauseOfDeath.HAPPINESS:
                    DisplayFinalMessage("Your pet has died of a broken heart! You should've played with it more.");
                    break;
            }
        }

        #endregion Life and Death

        #region Health and Pooping

        private int health;
        private const int HEALTH_MAX = 3;
        private Stopwatch poopTimer = new Stopwatch();
        private const int POOP_INTERVAL = 45 * 1000;
        private const int POOP_CHECK_INTERVAL = 5000;
        private const int CLEANING_ACTION_TIME = 7 * 1000;

        // Action by player
        private void Clean()
        {
            if (!isAlive)
            {
                DisplayFeedback("It's already dead. Cleaning it won't help.");
                return;
            }
            else if (!isAwake)
            {
                DisplayFeedback("It's asleep. You'll have to wake it up.");
                return;
            }
            acting = true;
            DisplayCurrentAction("Cleaning!", CLEANING_ACTION_TIME);
            health = HEALTH_MAX;
            DisplayStats();
        }

        // Part of the life cycle thread
        private void PoopMachine()
        {
            while (isAlive && !exit)
            {
                if (isAwake)
                {
                    if (poopTimer.ElapsedMilliseconds > POOP_INTERVAL)
                    {
                        health--;
                        if (health <= 0)
                        {
                            health = 0;
                            Die(CauseOfDeath.HEALTH);
                        }
                        DisplayFeedback("Gotta poop.");
                        DisplayStats();
                        poopTimer.Restart();
                    }
                }
                Thread.Sleep(POOP_CHECK_INTERVAL);
            }
        }

        #endregion Health and Pooping

        #region Hunger and Feeding

        private int food;
        private bool isFeeding = false;
        private const int FOOD_MAX = 50;
        private const int FOOD_RECOVERY = 15;
        private const int FOOD_DRAIN_INTERVAL = (45 * 1000) / FOOD_MAX;
        private const int FOOD_DRAIN_INTERVAL_WHILE_FEEDING = FOOD_DRAIN_INTERVAL * 2;
        private const int FEEDING_ACTION_TIME = 3000;
        private const int FOOD_DRAIN_DELAY_WHEN_FULL = 2500;

        // Part of the life cycle thread
        private void HungerDrain()
        {
            while (isAlive && !exit)
            {
                if (food >= FOOD_MAX)
                {
                    food = FOOD_MAX;
                    Thread.Sleep(FOOD_DRAIN_DELAY_WHEN_FULL);
                }
                food--;
                if (food <= 0)
                {
                    food = 0;
                    Die(CauseOfDeath.HUNGER);
                }
                DisplayStats();
                if (isFeeding)
                    Thread.Sleep(FOOD_DRAIN_INTERVAL_WHILE_FEEDING);
                else
                    Thread.Sleep(FOOD_DRAIN_INTERVAL);
            }
        }

        // Action by player
        private void Feed()
        {
            if (!isAlive)
            {
                DisplayFeedback("It's already dead. Food won't help.");
                return;
            }
            else if (!isAwake)
            {
                DisplayFeedback("It's asleep. You'll have to wake it up.");
                return;
            }
            acting = true;
            isFeeding = true;
            DisplayCurrentAction("Feeding!", FEEDING_ACTION_TIME);
            if (isAlive)
            {
                food += FOOD_RECOVERY;
                if (food >= FOOD_MAX)
                {
                    food = FOOD_MAX;
                    ForceSleep(CauseOfSleep.FOOD);
                }
                DisplayStats();
            }
            isFeeding = false;
        }

        #endregion Hunger and Feeding

        #region Energy and Sleep

        private int energy;
        private const int ENERGY_MAX = 50;
        private const int ENERGY_RECOVERY_INTERVAL = (45 * 1000) / ENERGY_MAX;
        private const int ENERGY_DRAIN_INTERVAL = (120 * 1000) / ENERGY_MAX;
        private const int SLEEP_MIN = 15 * 1000;
        private Stopwatch sleepTimer = new Stopwatch();
        private const int WAKE_MIN = 15 * 1000;
        private Stopwatch wakeTimer = new Stopwatch();
        private const int WAKING_ACTION_TIME = 1000;
        private const int BED_ACTION_TIME = 3000;
        private bool isAwake = true;

        // Part of the life cycle thread
        private void EnergyDrain()
        {
            while (isAlive && !exit)
            {
                if (isAwake)
                {
                    energy--;
                    DisplayStats();
                    if (energy <= 0)
                        ForceSleep(CauseOfSleep.ENERGY);
                }
                Thread.Sleep(ENERGY_DRAIN_INTERVAL);
            }
        }

        // Called when energy hits 0
        private void ForceSleep(int causeOfSleep)
        {
            switch (causeOfSleep)
            {
                case CauseOfSleep.FOOD:
                    DisplayFeedback("Wow, I'm stuffed! I'm taking a nap...");
                    break;

                case CauseOfSleep.ENERGY:
                    DisplayFeedback("So sleepy... I'm taking a nap...");
                    break;
            }
            Thread sleep = new Thread(Sleeping);
            sleep.Start();
        }

        // Action by player
        private void PutToBed()
        {
            if (!isAlive)
            {
                DisplayFeedback("It's dead, but you can pretend it's asleep if you'd like.");
                return;
            }
            if (!isAwake)
            {
                DisplayFeedback("It's already asleep!");
                return;
            }
            acting = true;
            DisplayCurrentAction("Putting to bed!", BED_ACTION_TIME);
            if (wakeTimer.ElapsedMilliseconds < WAKE_MIN)
            {
                // Grumpy
                DisplayFeedback("I don't want to go to bed! I just woke up!");
                happiness -= HAPPINESS_PENALTY;
                return;
            }
            DisplayFeedback("Sure! I love naps.");
            Thread sleep = new Thread(Sleeping);
            sleep.Start();
        }

        private void Sleeping()
        {
            isAwake = false;
            sleepTimer.Start();
            wakeTimer.Reset();
            while (isAlive && !exit)
            {
                if (!isAwake)
                {
                    energy++;
                    DisplayStats();
                    if (energy >= ENERGY_MAX)
                    {
                        ForceWake();
                    }
                }
                else
                    return;
                Thread.Sleep(ENERGY_RECOVERY_INTERVAL);
            }
        }

        // Energy full, forced awake
        private void ForceWake()
        {
            isAwake = true;
            sleepTimer.Reset();
            wakeTimer.Start();
            DisplayFeedback("That was a good nap! I feel so well rested!");
        }

        // Action by player
        private void Wake()
        {
            if (!isAlive)
            {
                DisplayFeedback("Yeah, that's not going to work. It's dead.");
                return;
            }
            if (isAwake)
            {
                DisplayFeedback("I'm already awake!");
                return;
            }
            acting = true;
            DisplayCurrentAction("Waking!", WAKING_ACTION_TIME);
            if (sleepTimer.ElapsedMilliseconds < SLEEP_MIN)
            {
                // Grumpy
                DisplayFeedback("I just got to sleep! Why did you wake me up so soon? *grumble* *grumble*");
                happiness -= HAPPINESS_PENALTY;
            }
            else
            {
                DisplayFeedback("Time to wake up? Okay, what are we doing?");
            }
            isAwake = true;
            sleepTimer.Reset();
            wakeTimer.Start();
        }

        #endregion Energy and Sleep

        #region Happiness and Playing

        private int happiness;
        private bool isPlaying = false;
        private const int HAPPINESS_MAX = 100;
        private const int HAPPINESS_RECOVERY = 25;
        private const int HAPPINESS_DRAIN_INTERVAL = (90 * 1000) / HAPPINESS_MAX;
        private const int HAPPINESS_DRAIN_INTERVAL_WHILE_PLAYING = HAPPINESS_DRAIN_INTERVAL * 2;
        private const int PLAYING_ACTION_TIME = 5000;
        private const int HAPPINESS_DRAIN_DELAY_WHEN_FULL = 2500;
        private const int HAPPINESS_PENALTY = 10;

        // Part of the life cycle thread
        private void HappinessDrain()
        {
            while (isAlive && !exit)
            {
                if (happiness >= HAPPINESS_MAX)
                {
                    happiness = HAPPINESS_MAX;
                    Thread.Sleep(HAPPINESS_DRAIN_DELAY_WHEN_FULL);
                }
                happiness--;
                if (happiness <= 0)
                {
                    happiness = 0;
                    Die(CauseOfDeath.HAPPINESS);
                }
                DisplayStats();
                if (isPlaying)
                    Thread.Sleep(HAPPINESS_DRAIN_INTERVAL_WHILE_PLAYING);
                else
                    Thread.Sleep(HAPPINESS_DRAIN_INTERVAL);
            }
        }

        // Action by player
        private void Play()
        {
            if (!isAlive)
            {
                DisplayFeedback("It's dead. You can't play with it.");
                return;
            }
            else if (!isAwake)
            {
                DisplayFeedback("It's asleep. You'll have to wake it up.");
                return;
            }
            acting = true;
            isPlaying = true;
            DisplayCurrentAction("Playing!", PLAYING_ACTION_TIME);
            if (isAlive)
            {
                happiness += HAPPINESS_RECOVERY;
                if (happiness >= HAPPINESS_MAX)
                {
                    happiness = HAPPINESS_MAX;
                    DisplayFeedback("I'm so happy! You're the best");
                }
                DisplayStats();
            }
            isPlaying = false;
        }

        #endregion Happiness and Playing

        #region Display

        private const int NUMBER_OF_ACTION_INTERVALS = 3;
        private const int FEEDBACK_INTERVAL = 3 * 1000;
        private const int FEEDBACK_RECHECK_INTERVAL = 250;
        private const int MIN_FEEDBACK_INTERVAL = 2 * 1000;
        private bool currentFeedback = false;

        private void DisplayCurrentAction(string action, int actionTime)
        {
            int actionInterval = actionTime / NUMBER_OF_ACTION_INTERVALS;
            string intervalText = "";
            int count = 0;
            do
            {
                WriteToScreen(LineNumber.ACTION, action + intervalText);
                intervalText += " .";
                Thread.Sleep(actionInterval);
                count++;
            } while (count <= NUMBER_OF_ACTION_INTERVALS && !exit && isAlive);
            ClearLine(LineNumber.ACTION);
            acting = false;
        }

        private void DisplayStats()
        {
            WriteToScreen(LineNumber.STATS, $"" +
                $"Age: {age,3}/{AGE_MAX}   " +
                $"Hunger: {food,2}/{FOOD_MAX}   " +
                $"Energy: {energy,2}/{ENERGY_MAX}   " +
                $"Health: {health}/{HEALTH_MAX}   " +
                $"Happiness: {happiness,3}/{HAPPINESS_MAX}");
        }

        private void DisplayInstructions()
        {
            WriteToScreen(LineNumber.INSTRUCTIONS, "(F)eed / (B)ed / (W)ake / (C)lean / (P)lay / Esc to exit");
        }

        private void DisplayFeedback(string feedback)
        {
            Thread feedbackThread = new Thread(() => FeedbackDelay(feedback));
            feedbackThread.Start();
        }

        private void FeedbackDelay(string feedback)
        {
            while (currentFeedback)
            {
                Thread.Sleep(FEEDBACK_RECHECK_INTERVAL);
            }
            currentFeedback = true;
            ClearLine(LineNumber.FEEDBACK);
            WriteToScreen(LineNumber.FEEDBACK, feedback);
            Thread.Sleep(MIN_FEEDBACK_INTERVAL);
            currentFeedback = false;
            Thread.Sleep(FEEDBACK_INTERVAL);
            if (!currentFeedback)
            {
                ClearLine(LineNumber.FEEDBACK);
            }
        }

        private void DisplayFinalMessage(string message)
        {
            ClearLine(LineNumber.FINALMESSAGE);
            WriteToScreen(LineNumber.FINALMESSAGE, message);
        }

        private void WriteToScreen(int lineNumber, string display)
        {
            lock (locker)
            {
                Console.SetCursorPosition(0, lineNumber);
                Console.Write(display);
                Console.SetCursorPosition(0, LineNumber.CURSOR);
            }
        }

        private void ClearLine(int lineNumber)
        {
            WriteToScreen(lineNumber, new string(' ', Console.BufferWidth));
        }

        #endregion Display
    }
}