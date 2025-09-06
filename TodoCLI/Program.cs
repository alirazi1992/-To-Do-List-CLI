using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TodoCLI
{
    class Program
    {
        static void Main()
        {
            Console.Title = "To-Do List CLI - Day 4";
            var store = new TodoStore("todo.json");
            store.Load(); // load if file exists

            while (true)
            {
                ShowMenu();
                switch (ReadChoice())
                {
                    case "1": AddTask(store); break;
                    case "2": ListTasks(store, showAll: true); break;
                    case "3": MarkDone(store); break;
                    case "4": DeleteTask(store); break;
                    case "5": ToggleDone(store); break;
                    case "6": ClearCompleted(store); break;
                    case "7": store.Save(); Notify("Saved ✅"); break;
                    case "0":
                        store.Save();
                        Console.WriteLine("Bye! 👋");
                        return;
                    default:
                        Warn("Unknown option. Try again.");
                        break;
                }
            }
        }

        static void ShowMenu()
        {
            Console.WriteLine();
            Console.WriteLine("=== TO-DO LIST ===");
            Console.WriteLine("1) Add task");
            Console.WriteLine("2) List tasks");
            Console.WriteLine("3) Mark task as done");
            Console.WriteLine("4) Delete task");
            Console.WriteLine("5) Toggle done/undone");
            Console.WriteLine("6) Clear completed");
            Console.WriteLine("7) Save now");
            Console.WriteLine("0) Exit");
            Console.Write("Choose: ");
        }

        static string ReadChoice() => (Console.ReadLine() ?? "").Trim();

        static void AddTask(TodoStore store)
        {
            Console.Write("Task title: ");
            var title = (Console.ReadLine() ?? "").Trim();
            if (title.Length == 0)
            {
                Warn("Title cannot be empty.");
                return;
            }

            Console.Write("Optional notes (enter to skip): ");
            var notes = Console.ReadLine() ?? "";

            store.Add(new TodoItem { Title = title, Notes = notes });
            store.Save();
            Notify("Added ✅");
        }

        static void ListTasks(TodoStore store, bool showAll = true)
        {
            if (store.Items.Count == 0)
            {
                Info("No tasks yet. Add one with option 1.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("#  Status  Title                             Created");
            Console.WriteLine("-- ------- --------------------------------- ---------------------");

            for (int i = 0; i < store.Items.Count; i++)
            {
                var t = store.Items[i];
                var status = t.Done ? "[x]" : "[ ]";
                Console.WriteLine($"{i + 1,2} {status,-7} {Trunc(t.Title, 33),-33} {t.CreatedAt:g}");
                if (!string.IsNullOrWhiteSpace(t.Notes))
                {
                    Console.WriteLine($"    ↳ {Trunc(t.Notes, 60)}");
                }
            }
        }

        static void MarkDone(TodoStore store)
        {
            if (!TryPickIndex(store, "Mark which task as done (number): ", out int idx)) return;
            store.Items[idx].Done = true;
            store.Items[idx].CompletedAt = DateTime.Now;
            store.Save();
            Notify("Marked as done ✅");
        }

        static void ToggleDone(TodoStore store)
        {
            if (!TryPickIndex(store, "Toggle which task (number): ", out int idx)) return;
            var item = store.Items[idx];
            item.Done = !item.Done;
            item.CompletedAt = item.Done ? DateTime.Now : null;
            store.Save();
            Notify(item.Done ? "Marked done ✅" : "Marked undone 🔄");
        }

        static void DeleteTask(TodoStore store)
        {
            if (!TryPickIndex(store, "Delete which task (number): ", out int idx)) return;
            var title = store.Items[idx].Title;
            store.Items.RemoveAt(idx);
            store.Save();
            Notify($"Deleted “{title}” 🗑️");
        }

        static void ClearCompleted(TodoStore store)
        {
            int before = store.Items.Count;
            store.Items.RemoveAll(t => t.Done);
            int removed = before - store.Items.Count;
            store.Save();
            Notify(removed > 0 ? $"Removed {removed} completed task(s)." : "No completed tasks to clear.");
        }

        static bool TryPickIndex(TodoStore store, string prompt, out int idx)
        {
            idx = -1;
            if (store.Items.Count == 0)
            {
                Warn("There are no tasks.");
                return false;
            }

            ListTasks(store, showAll: true);
            Console.Write(prompt);
            var input = Console.ReadLine();

            if (int.TryParse(input, out int n) && n >= 1 && n <= store.Items.Count)
            {
                idx = n - 1;
                return true;
            }

            Warn("Invalid number.");
            return false;
        }

        static string Trunc(string s, int len) => s.Length <= len ? s : s[..(len - 1)] + "…";
        static void Warn(string msg) { Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine(msg); Console.ResetColor(); }
        static void Notify(string msg) { Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine(msg); Console.ResetColor(); }
        static void Info(string msg) { Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine(msg); Console.ResetColor(); }
    }

    class TodoItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "";
        public string? Notes { get; set; }
        public bool Done { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }

    class TodoStore
    {
        public List<TodoItem> Items { get; private set; } = new();
        private readonly string _path;
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public TodoStore(string path) { _path = path; }

        public void Add(TodoItem item) => Items.Add(item);

        public void Save()
        {
            var json = JsonSerializer.Serialize(Items, JsonOpts);
            File.WriteAllText(_path, json);
        }

        public void Load()
        {
            if (!File.Exists(_path)) return;
            var json = File.ReadAllText(_path);
            var loaded = JsonSerializer.Deserialize<List<TodoItem>>(json);
            if (loaded != null) Items = loaded;
        }
    }
}
