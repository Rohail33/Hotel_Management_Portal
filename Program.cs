using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Raylib_cs;
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Contact { get; set; }
    public string Email { get; set; }

    public Customer(int id, string name, string contact, string email)
    {
        Id = id;
        Name = name;
        Contact = contact;
        Email = email;
    }

    public string ToFileString() => $"{Id},{Name},{Contact},{Email}";

    public static Customer FromFileString(string data)
    {
        var parts = data.Split(',');
        return new Customer(int.Parse(parts[0]), parts[1], parts[2], parts[3]);
    }
}

public class CustomerManager
{
    private List<Customer> customers = new List<Customer>();
    private string filePath = "data/customers.txt";

    public CustomerManager()
    {
        Directory.CreateDirectory("data");
        LoadCustomers();
    }

    private void LoadCustomers()
    {
        if (File.Exists(filePath))
        {
            customers.AddRange(Array.ConvertAll(File.ReadAllLines(filePath), Customer.FromFileString));
        }
    }

    private void SaveCustomers() => File.WriteAllLines(filePath, customers.ConvertAll(c => c.ToFileString()));

    public void AddCustomer(string name, string contact, string email)
    {
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(contact))
        {
            customers.Add(new Customer(customers.Count > 0 ? customers[^1].Id + 1 : 1, name, contact, email));
            SaveCustomers();
        }
    }

    public List<Customer> GetAllCustomers() => customers;
    public Customer GetCustomerById(int id) => customers.Find(c => c.Id == id);

    public void DeleteCustomer(int id)
    {
        var customer = customers.Find(c => c.Id == id);
        if (customer != null)
        {
            customers.Remove(customer);
            SaveCustomers();
        }
    }
}

public class Room
{
    public int Number { get; set; }
    public string Type { get; set; }
    public bool IsOccupied { get; set; }
    public int? CustomerId { get; set; }

    public Room(int number, string type)
    {
        Number = number;
        Type = type;
        IsOccupied = false;
        CustomerId = null;
    }

    public string ToFileString()
    {
        return $"{Number},{Type},{IsOccupied},{CustomerId?.ToString() ?? ""}";
    }

    public static Room FromFileString(string data)
    {
        var parts = data.Split(',');
        var room = new Room(int.Parse(parts[0]), parts[1]);
        room.IsOccupied = bool.Parse(parts[2]);
        room.CustomerId = string.IsNullOrEmpty(parts[3]) ? null : int.Parse(parts[3]);
        return room;
    }
}

public class RoomManager
{
    private List<Room> rooms = new List<Room>();
    private string filePath = "data/rooms.txt";
    private void InitializeRooms(int totalRooms)
    {
        rooms.Clear();
        for (int i = 1; i <= totalRooms; i++)
        {
            string type = i <= totalRooms / 3 ? "Single" :
                         i <= 2 * totalRooms / 3 ? "Double" : "Suite";
            rooms.Add(new Room(i, type));
        }
        SaveRooms();
    }
    private void LoadRooms()
    {
        rooms.Clear();
        HashSet<int> seen = new HashSet<int>();
        var lines = File.ReadAllLines(filePath);
        foreach (var line in lines)
        {
            try
            {
                var room = Room.FromFileString(line);
                if (!seen.Contains(room.Number))
                {
                    rooms.Add(room);
                    seen.Add(room.Number);
                }
            }
            catch
            {
                // For Preventing Duplication
            }
        }
    }
    private void SaveRooms()
    {
        File.WriteAllLines(filePath, rooms.ConvertAll(r => r.ToFileString()));
    }
    public RoomManager(int totalRooms)
    {
        Directory.CreateDirectory("data");

        if (File.Exists(filePath))
            LoadRooms();
        else
            InitializeRooms(totalRooms);
    }

    public List<Room> GetAvailableRooms() =>
    rooms.Where(r => !r.IsOccupied)
         .OrderBy(r => r.Number)
         .ToList();
    public List<Room> GetOccupiedRooms() => rooms.FindAll(r => r.IsOccupied);

    public bool BookRoom(int roomNumber, int customerId)
    {
        var room = rooms.Find(r => r.Number == roomNumber);
        if (room == null || room.IsOccupied) return false;

        room.IsOccupied = true;
        room.CustomerId = customerId;
        SaveRooms();
        return true;
    }

    public bool CheckOut(int roomNumber)
    {
        var room = rooms.Find(r => r.Number == roomNumber);
        if (room == null || !room.IsOccupied) return false;

        room.IsOccupied = false;
        room.CustomerId = null;
        SaveRooms();
        return true;
    }

    public Room? GetRoomByNumber(int roomNumber)
    {
        return rooms.Find(r => r.Number == roomNumber);
    }
}

public class Bill
{
    public string CustomerName { get; set; }
    public string RoomType { get; set; }
    public float TotalAmount { get; set; }

    public class Item
    {
        public string Name;
        public double Price;
        public int Quantity;
        public double Total => Price * Quantity;
    }

    private List<Item> items = new List<Item>();
    private double totalAmount = 0;
    private double discount = 0;
    private double amountPaid = 0;

    public void AddItem(string name, double price, int qty)
    {
        items.Add(new Item { Name = name, Price = price, Quantity = qty });
        totalAmount += price * qty;
    }
    public string GetSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== BILL ===");
        foreach (var item in items)
            sb.AppendLine($"{item.Name}: Rs {item.Price} x {item.Quantity} = Rs {item.Total}");
        sb.AppendLine($"Subtotal: Rs {totalAmount}");
        sb.AppendLine($"Discount: Rs {discount}");
        sb.AppendLine($"Grand Total: Rs {totalAmount - discount}");
        return sb.ToString();
    }

    public void ApplyDiscount(double pct) => discount = (pct / 100) * totalAmount;

    public string ProcessPayment(double pay)
    {
        double grand = totalAmount - discount;
        if (pay < grand)
            return $"Need Rs {grand - pay:0.00} more.";
        amountPaid = pay;
        return $"Paid! Change: Rs {pay - grand:0.00}";
    }

    public void Reset()
    {
        items.Clear();
        totalAmount = discount = amountPaid = 0;
    }

    public class BillManager
    {
        private RoomManager roomManager;
        private CustomerManager customerManager;

        public BillManager(RoomManager roomMgr, CustomerManager customerMgr)
        {
            roomManager = roomMgr;
            customerManager = customerMgr;
        }

        public Bill GenerateBill(int roomNumber)
        {
            var room = roomManager.GetRoomByNumber(roomNumber);
            if (room == null || !room.IsOccupied || room.CustomerId == null)
                return null;

            var customer = customerManager.GetCustomerById(room.CustomerId.Value);

            float rate = room.Type switch
            {
                "Single" => 3000,
                "Double" => 4000,
                "Suite" => 5000,
                _ => 3000
            };

            return new Bill
            {
                CustomerName = customer?.Name ?? "Unknown",
                RoomType = room.Type,
                TotalAmount = rate
            };
        }
    }
}

public class HotelSystem
{
    private CustomerManager customerManager;
    private RoomManager roomManager;
    private Bill.BillManager billManager;
    private Bill generatedBill = new Bill();
    private string inputBillingRoomNumber = "";
    private int currentScreen = 0;
    private int selectedOption = 0;
    private int subMenuOption = -1;
    private int? hoveredDeleteId = null;
    private int? confirmDeleteCustomerId = null;
    private string confirmDeleteCustomerName = "";
    private bool musicEnabled = false; // Make the BG sound disabled by default
    private double lastHoverSoundTime = 0;
    private const double hoverSoundCooldown = 0.1; // seconds (100 ms)
    private string bookingErrorMessage = "";
    private double bookingErrorTime = 0;
    // Fonts
    private Font fontRegular;
    private Font fontBold;
    // Background Image
    private Texture2D backgroundTexture;
    // Sound effects
    private Sound hoverSound;
    private Sound selectSound;
    private int lastHoveredIndex = -1;
    // Background Music
    private Music backgroundMusic;

    // Input fields
    private string inputName = "", inputContact = "", inputEmail = "";
    private string inputRoomNumber = "", inputCustomerId = "", inputCheckoutRoom = "";

    private float blinkTimer = 0;
    private bool showCursor = true;
    private bool isInputActive = false;
    private string activeInputField = "";

    // Menu options
    private readonly List<string> mainMenuOptions = new List<string> {
        "Customer Management", "Room Management", "Billing","Reports", "Credits", "Exit"
    };

    private readonly List<string> customerMenuOptions = new List<string> {
        "Add Customer", "View Customers", "Back"
    };

    private readonly List<string> roomMenuOptions = new List<string> {
        "View Available Rooms", "Book Room", "Check Out", "Back"
    };

    private readonly List<(string Name, string Roll)> credits = new List<(string, string)> {
        ("Rohail Khan", "Lead Developer"),
        ("Salim Arif", "Co-Lead Developer"),
    };

    public HotelSystem()
    {
        customerManager = new CustomerManager();
        roomManager = new RoomManager(15);
        billManager = new Bill.BillManager(roomManager, customerManager);
    }

    public void Run()
    {
        Raylib.InitWindow(1024, 768, "Hotel Management Portal");
        backgroundTexture = Raylib.LoadTexture("assets/Background.jpg"); // Load the BG
        // Load the fonts
        fontRegular = Raylib.LoadFont("fonts/Roboto-Regular.ttf"); 
        fontBold = Raylib.LoadFont("fonts/Roboto-Bold.ttf");
        // Load the Sound effect
        Raylib.InitAudioDevice();
        hoverSound = Raylib.LoadSound("assets/Select_effect.wav");
        selectSound = Raylib.LoadSound("assets/Enter_effect.wav");
        // Load Background Music
        backgroundMusic = Raylib.LoadMusicStream("assets/background_music.wav");
        // Set volume lower (0.0 to 1.0)
        Raylib.SetMusicVolume(backgroundMusic, 0.4f); // 40% volume

        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();
            Update(deltaTime);
            Draw();
        }

        // Unload the Fonts 
        Raylib.UnloadFont(fontRegular);
        Raylib.UnloadFont(fontBold);
        Raylib.UnloadTexture(backgroundTexture);  // Unload the BG picture
        // Unload the Sound effect
        Raylib.UnloadSound(hoverSound);
        Raylib.UnloadSound(selectSound);
        Raylib.CloseAudioDevice();
        // Unload Backgroud Music
        Raylib.StopMusicStream(backgroundMusic);
        Raylib.UnloadMusicStream(backgroundMusic);

        Raylib.CloseWindow();
    }

    private void Update(float deltaTime)
    {
        blinkTimer += deltaTime;
        if (blinkTimer >= 0.5f)
        {
            blinkTimer = 0;
            showCursor = !showCursor;
        }

        Raylib.UpdateMusicStream(backgroundMusic); // Ensuring smooth PlayBack
        HandleInput();
    }

    private void HandleInput()
    {
        if (isInputActive)
        {
            HandleTextInput();
            return;
        }

        // ESC always goes back
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            if (subMenuOption != -1) subMenuOption = -1;
            else if (currentScreen != 0) currentScreen = 0;
            ClearInputFields();
            return;
        }

        // Keyboard navigation
        var opt = GetCurrentOptions();
        int oldOption = selectedOption;
        if (Raylib.IsKeyPressed(KeyboardKey.Down))
            selectedOption = (selectedOption + 1) % opt.Count;

        if (Raylib.IsKeyPressed(KeyboardKey.Up))
            selectedOption = (selectedOption - 1 + opt.Count) % opt.Count;

        if (selectedOption != oldOption)
        {
            Raylib.PlaySound(hoverSound);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Enter) && !isInputActive && subMenuOption == -1)
        {
            Raylib.PlaySound(selectSound);
            ProcessSelection(); 
        }

        // Mouse interaction
        Vector2 mousePos = Raylib.GetMousePosition();
        var options = GetCurrentOptions();

        if (subMenuOption == -1 && !isInputActive)
        {
            for (int i = 0; i < options.Count; i++)
            {
                Rectangle optionRect = new Rectangle(100, 180 + i * 60, 400, 50);
                if (Raylib.CheckCollisionPointRec(mousePos, optionRect))
                {
                    if (selectedOption != i)
                    {
                        selectedOption = i;
                        Raylib.PlaySound(hoverSound); // Play effect at first of the hover
                    }

                    if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        Raylib.PlaySound(selectSound); // Play the sound on Click
                        ProcessSelection();
                    }
                    break;
                }
            }
        }
    }
    private void ProcessSelection()
    {
        if (currentScreen == 0) // Main menu
        {
            if (selectedOption == mainMenuOptions.Count - 1) Raylib.CloseWindow();
            else currentScreen = selectedOption + 1;
            selectedOption = 0;
        }
        else if (currentScreen == 1) // Customer menu
        {
            if (selectedOption == customerMenuOptions.Count - 1) currentScreen = 0;
            else subMenuOption = selectedOption;
        }
        else if (currentScreen == 2) // Room menu
        {
            if (selectedOption == roomMenuOptions.Count - 1) currentScreen = 0;
            else subMenuOption = selectedOption;
        }
        else if (currentScreen == 3) // Billing
        {
            currentScreen = 0;
        }
        else if (currentScreen == 4 || currentScreen == 5) // Reports or Credits
        {
            currentScreen = 0;
        }
    }
    private void Draw()
    {
        if (musicEnabled)
            Raylib.UpdateMusicStream(backgroundMusic);
        Raylib.BeginDrawing();
        // For our Custom Background Picture
        Raylib.DrawTexturePro(
            backgroundTexture,
            new Rectangle(0, 0, backgroundTexture.Width, backgroundTexture.Height),
            new Rectangle(0, 0, 1024, 768),
            new Vector2(0, 0),
            0f,
            Color.White
        );
        // Header
        Raylib.DrawTextEx(fontBold, "Hotel Management System", new Vector2(50, 30), 40, 1, Color.DarkGray);

        if (currentScreen != 0)
        {
            string backText = "< Back";
            int textWidth = Raylib.MeasureText(backText, 24);
            Rectangle backBtn = new Rectangle(20, 80, textWidth + 20, 40);
            bool hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), backBtn);

            if (hovered && lastHoveredIndex != -999)
            {
                double now = Raylib.GetTime();
                if (now - lastHoverSoundTime > hoverSoundCooldown)
                {
                    Raylib.PlaySound(hoverSound);
                    lastHoverSoundTime = now;
                }
                lastHoveredIndex = -999;
            }
            else if (!hovered && lastHoveredIndex == -999)
            {
                lastHoveredIndex = -1;
            }

            Raylib.DrawRectangleRec(backBtn, hovered ? Color.LightGray : Color.White);
            Raylib.DrawRectangleLinesEx(backBtn, 1, Color.Gray);
            Raylib.DrawTextEx(fontRegular, backText,
                new Vector2((int)(backBtn.X + 10),
                (int)(backBtn.Y + (backBtn.Height - 24) / 2)),
                24, 1, Color.Black);

            if (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                double now = Raylib.GetTime();
                if (now - lastHoverSoundTime > hoverSoundCooldown)
                {
                    Raylib.PlaySound(hoverSound);
                    lastHoverSoundTime = now;
                }
                if (subMenuOption != -1)
                    subMenuOption = -1;
                else 
                    currentScreen = 0;
                ClearInputFields();
            }
        }

        // Current screen content
        switch (currentScreen)
        {
            case 0: 
                DrawMainMenu();
                break;
            case 1:
                DrawCustomerMenu();
                break;
            case 2:
                DrawRoomMenu();
                break;
            case 3:
                DrawBillingMenu();
                break;
            case 4:
                DrawReports();
                break;
            case 5:
                DrawCredits();
                break;
        }

        Raylib.EndDrawing();
    }
    private void DrawMainMenu()
    {
        Rectangle musicBtn = new Rectangle(850, 30, 120, 40);
        bool musicHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), musicBtn);

        Raylib.DrawRectangleRec(musicBtn, musicHover ? Color.LightGray : Color.White);
        Raylib.DrawRectangleLinesEx(musicBtn, 1, Color.Black);
        Raylib.DrawTextEx(fontBold,musicEnabled ? "Music: ON" : "Music: OFF", new Vector2((int)(musicBtn.X + 10), (int)(musicBtn.Y + 10)), 20, 1, Color.Black);

        if (musicHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            musicEnabled = !musicEnabled;
            Raylib.PlaySound(selectSound);

            if (musicEnabled)
                Raylib.PlayMusicStream(backgroundMusic);
            else
                Raylib.StopMusicStream(backgroundMusic);
        }
        bool hoveredAnyOption = false; // New flag

        for (int i = 0; i < mainMenuOptions.Count; i++)
        {
            Rectangle optionRect = new Rectangle(100, 180 + i * 60, 400, 50);
            bool isHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), optionRect);
            bool isSelected = i == selectedOption;

            if (isSelected)
                Raylib.DrawRectangleLinesEx(optionRect, 2, Color.Blue);

            Raylib.DrawTextEx(fontBold, mainMenuOptions[i],
                new Vector2(optionRect.X + 20, optionRect.Y + 15),
                28, 1, Color.Black);

            if (isHovered)
            {
                hoveredAnyOption = true;

                if (lastHoveredIndex != i)
                {
                    double now = Raylib.GetTime();
                    if (now - lastHoverSoundTime > hoverSoundCooldown)
                    {
                        Raylib.PlaySound(hoverSound);
                        lastHoverSoundTime = now;
                    }
                    lastHoveredIndex = i;
                }
            }
        }

        if (!hoveredAnyOption)
            lastHoveredIndex = -1;
    }
    private void DrawCustomerMenu()
    {
        if (subMenuOption == -1)
        {
            for (int i = 0; i < customerMenuOptions.Count; i++)
            {
                bool isSelected = i == selectedOption;
                Rectangle optionRect = new Rectangle(100, 180 + i * 60, 400, 50);

                if (isSelected)
                {
                    Raylib.DrawRectangleLinesEx(optionRect, 2, Color.Blue);
                }

                Raylib.DrawTextEx(fontBold, customerMenuOptions[i],
                    new Vector2((int)optionRect.X + 20,
                    (int)optionRect.Y + 15),
                    28, 1, Color.Black);
            }
        }
        else
        {
            switch (subMenuOption)
            {
                case 0: DrawAddCustomer(); break;
                case 1: DrawViewCustomers(); break;
            }
        }
    }
    private void DrawAddCustomer()
    {
        // Form background
        Raylib.DrawRectangle(100, 180, 800, 400, Color.White);
        Raylib.DrawRectangleLines(100, 180, 800, 400, Color.Gray);
        Raylib.DrawTextEx(fontBold, "Add New Customer", new Vector2(120, 200), 30, 1, Color.DarkGray);

        // Input fields
        DrawInputField("Name:", inputName, 250, "name");
        DrawInputField("Contact:", inputContact, 320, "contact");
        DrawInputField("Email:", inputEmail, 390, "email");

        // Submit button
        Rectangle submitBtn = new Rectangle(120, 450, 200, 45);
        bool submitHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), submitBtn);
        // Also play sound effect on submit button
        if (submitHover && lastHoveredIndex != 101)
        {
            Raylib.PlaySound(hoverSound);
            lastHoveredIndex = 101;
        }
        else if (!submitHover && lastHoveredIndex == 101)
        {
            lastHoveredIndex = -1;
        }

        Raylib.DrawRectangleRec(submitBtn, submitHover ? Color.LightGray : Color.White);
        Raylib.DrawRectangleLinesEx(submitBtn, 1, Color.Gray);
        Raylib.DrawTextEx(fontRegular, "Submit",
            new Vector2((int)(submitBtn.X + (submitBtn.Width - Raylib.MeasureText("Submit", 24)) / 2),
            (int)(submitBtn.Y + (submitBtn.Height - 24) / 2)),
            24, 1, Color.Black);

        if (submitHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Raylib.PlaySound(selectSound);
            customerManager.AddCustomer(inputName, inputContact, inputEmail);
            ClearInputFields();
            subMenuOption = -1;
        }

        // Cancel button
        Rectangle cancelBtn = new Rectangle(350, 450, 200, 45);
        bool cancelHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), cancelBtn);
        // Also play sound effects on cancel button
        if (cancelHover && lastHoveredIndex != 102)
        {
            Raylib.PlaySound(hoverSound);
            lastHoveredIndex = 102;
        }
        else if (!cancelHover && lastHoveredIndex == 102)
        {
            lastHoveredIndex = -1;
        }
        Raylib.DrawRectangleRec(cancelBtn, cancelHover ? Color.LightGray : Color.White);
        Raylib.DrawRectangleLinesEx(cancelBtn, 1, Color.Gray);
        Raylib.DrawTextEx(fontRegular, "Cancel",
            new Vector2((int)(cancelBtn.X + (cancelBtn.Width - Raylib.MeasureText("Cancel", 24)) / 2),
            (int)(cancelBtn.Y + (cancelBtn.Height - 24) / 2)),
            24, 1, Color.Black);

        if (cancelHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Raylib.PlaySound(selectSound);
            ClearInputFields();
            subMenuOption = -1;
        }
    }

    private void DrawViewCustomers()
    {
        Raylib.DrawRectangle(100, 180, 800, 500, Color.White);
        Raylib.DrawRectangleLines(100, 180, 800, 500, Color.Gray);
        Raylib.DrawTextEx(fontBold, "All Customers", new Vector2(120, 200), 30, 1, Color.DarkGray);
        // Column Headings
        Raylib.DrawTextEx(fontBold, "ID", new Vector2(140, 240), 22, 1, Color.DarkGray);
        Raylib.DrawTextEx(fontBold, "Name", new Vector2(200, 240), 22, 1, Color.DarkGray);
        Raylib.DrawTextEx(fontBold, "Contact", new Vector2(450, 240), 22, 1, Color.DarkGray);
        Raylib.DrawTextEx(fontBold, "Email", new Vector2(650, 240), 22, 1, Color.DarkGray);
        Raylib.DrawLine(120, 265, 880, 265, Color.Gray);
        int y = 270;
        hoveredDeleteId = null;
        foreach (var customer in customerManager.GetAllCustomers())
        {
            Raylib.DrawTextEx(fontRegular, $"{customer.Id}", new Vector2(140, y), 20, 1, Color.Black);
            Raylib.DrawTextEx(fontRegular, customer.Name, new Vector2(200, y), 20, 1, Color.Black);
            Raylib.DrawTextEx(fontRegular, customer.Contact, new Vector2(450, y), 20, 1, Color.Black);
            Raylib.DrawTextEx(fontRegular, customer.Email, new Vector2(650, y), 20, 1, Color.Black);
            // Delete Button
            Rectangle delBtn = new Rectangle(860, y, 20, 20);
            bool hover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), delBtn);
            if (hover && lastHoveredIndex != 601)
            {
                double now = Raylib.GetTime();
                if (now - lastHoverSoundTime > hoverSoundCooldown)
                {
                    Raylib.PlaySound(hoverSound);
                    lastHoverSoundTime = now;
                }
                lastHoveredIndex = 601;
            }
            else if (!hover && lastHoveredIndex == 601)
            {
                lastHoveredIndex = -1;
            }

            Raylib.DrawRectangleRec(delBtn, hover ? Color.Red : Color.LightGray);
            Raylib.DrawRectangleLinesEx(delBtn, 1, Color.Black);
            Raylib.DrawText("X", (int)(delBtn.X + 5), (int)(delBtn.Y + 2), 16, Color.White);

            if (hover)
            {
                hoveredDeleteId = customer.Id;

                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    // Set confirmation state instead of direct delete
                    confirmDeleteCustomerId = customer.Id;
                    confirmDeleteCustomerName = customer.Name;
                }
            }
            if (confirmDeleteCustomerId.HasValue)
            {
                Rectangle box = new Rectangle(300, 300, 400, 180);
                Raylib.DrawRectangleRec(box, Color.White);
                Raylib.DrawRectangleLinesEx(box, 2, Color.Black);

                string msg = $"Delete \"{confirmDeleteCustomerName}\"?";
                Raylib.DrawTextEx(fontBold, msg, new Vector2(320, 330), 22, 1, Color.DarkGray);

                Rectangle yesBtn = new Rectangle(320, 380, 100, 40);
                Rectangle cancelBtn = new Rectangle(460, 380, 100, 40);

                bool yesHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), yesBtn);
                bool cancelHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), cancelBtn);

                if (yesHover && lastHoveredIndex != 602)
                {
                    Raylib.PlaySound(hoverSound);
                    lastHoveredIndex = 602;
                }
                else if (!yesHover && lastHoveredIndex == 602)
                {
                    lastHoveredIndex = -1;
                }

                if (cancelHover && lastHoveredIndex != 603)
                {
                    Raylib.PlaySound(hoverSound);
                    lastHoveredIndex = 603;
                }
                else if (!cancelHover && lastHoveredIndex == 603)
                {
                    lastHoveredIndex = -1;
                }

                Raylib.DrawRectangleRec(yesBtn, yesHover ? Color.LightGray : Color.White);
                Raylib.DrawRectangleLinesEx(yesBtn, 1, Color.Black);
                Raylib.DrawTextEx(fontRegular,"Yes", new Vector2((int)(yesBtn.X + 30), (int)(yesBtn.Y + 10)), 20, 1, Color.Black);

                Raylib.DrawRectangleRec(cancelBtn, cancelHover ? Color.LightGray : Color.White);
                Raylib.DrawRectangleLinesEx(cancelBtn, 1, Color.Black);
                Raylib.DrawTextEx(fontRegular, "Cancel", new Vector2((int)(cancelBtn.X + 10), (int)(cancelBtn.Y + 10)), 20, 1, Color.Black);

                if (yesHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    Raylib.PlaySound(selectSound);
                    int idToDelete = confirmDeleteCustomerId.Value;
                    confirmDeleteCustomerId = null;
                    confirmDeleteCustomerName = "";

                    Raylib.EndDrawing();
                    customerManager.DeleteCustomer(idToDelete);
                    return;
                }

                if (cancelHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    confirmDeleteCustomerId = null;
                    confirmDeleteCustomerName = "";
                    Raylib.PlaySound(hoverSound);
                }
            }
            y += 30;
        }

        if (customerManager.GetAllCustomers().Count == 0)
        {
            Raylib.DrawTextEx(fontBold, "No customers found", new Vector2(140, 270), 24, 1, Color.Gray);
        }
    }

    private void DrawRoomMenu()
    {
        if (subMenuOption == -1)
        {
            for (int i = 0; i < roomMenuOptions.Count; i++)
            {
                bool isSelected = i == selectedOption;
                Rectangle optionRect = new Rectangle(100, 180 + i * 60, 400, 50);

                if (isSelected)
                {
                    Raylib.DrawRectangleLinesEx(optionRect, 2, Color.Blue);
                }

                Raylib.DrawTextEx(fontBold, roomMenuOptions[i],
                    new Vector2((int)optionRect.X + 20,
                    (int)optionRect.Y + 15),
                    28, 1, Color.Black);
            }
        }
        else
        {
            switch (subMenuOption)
            {
                case 0: DrawAvailableRooms(); break;
                case 1: DrawBookRoom(); break;
                case 2: DrawCheckOut(); break;
            }
        }
    }
    private void DrawAvailableRooms()
    {
        // Card background
        Raylib.DrawRectangle(100, 180, 800, 500, Color.White);
        Raylib.DrawRectangleLines(100, 180, 800, 500, Color.Gray);
        Raylib.DrawTextEx(fontBold, "Available Rooms", new Vector2(120, 200), 30, 1, Color.DarkGray);

        // Display rooms in a grid
        var rooms = roomManager.GetAvailableRooms();
        int x = 140, y = 250;
        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            Rectangle roomRect = new Rectangle(x, y, 120, 80);
            Raylib.DrawRectangleLinesEx(roomRect, 1, Color.LightGray);
            Raylib.DrawTextEx(fontRegular, $"Room {room.Number}", new Vector2(x + 10, y + 10), 20, 1, Color.Black);
            Raylib.DrawTextEx(fontRegular, room.Type, new Vector2(x + 10, y + 40), 18, 1, Color.DarkGray);

            x += 140;
            if ((i + 1) % 5 == 0) { x = 140; y += 100; }
        }
        if (rooms.Count == 0)
        {
            Raylib.DrawTextEx(fontBold, "No available rooms", new Vector2(140, 270), 24, 1, Color.Gray);
        }
    }
    private void DrawBookRoom()
    {
        Raylib.DrawRectangle(100, 180, 800, 400, Color.White);
        Raylib.DrawRectangleLines(100, 180, 800, 400, Color.Gray);
        Raylib.DrawTextEx(fontBold, "Book a Room", new Vector2(120, 200), 30, 1, Color.DarkGray);

        DrawInputField("Room Number:", inputRoomNumber, 250, "roomNumber");
        DrawInputField("Customer ID:", inputCustomerId, 320, "customerId");

        // Submit button
        Rectangle submitBtn = new Rectangle(120, 400, 200, 45);
        bool submitHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), submitBtn);
        Raylib.DrawRectangleRec(submitBtn, submitHover ? Color.LightGray : Color.White);
        // Hover sound
        if (submitHover && lastHoveredIndex != 201)
        {
            Raylib.PlaySound(hoverSound);
            lastHoveredIndex = 201;
        }
        else if (!submitHover && lastHoveredIndex == 201)
        {
            lastHoveredIndex = -1;
        }
        Raylib.DrawRectangleLinesEx(submitBtn, 1, Color.Gray);
        Raylib.DrawTextEx(fontRegular, "Submit",
            new Vector2((int)(submitBtn.X + (submitBtn.Width - Raylib.MeasureText("Submit", 24)) / 2),
            (int)(submitBtn.Y + (submitBtn.Height - 24) / 2)),
            24, 1, Color.Black);

        if (submitHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Raylib.PlaySound(selectSound);
            if (int.TryParse(inputRoomNumber, out int room) && int.TryParse(inputCustomerId, out int customer))
            {
                if (roomManager.BookRoom(room, customer))
                {
                    bookingErrorMessage = "";
                    ClearInputFields();
                    subMenuOption = -1;
                }
                else
                {
                    bookingErrorMessage = $"Error: Room {room} is already booked.";
                    bookingErrorTime = Raylib.GetTime();
                    Raylib.PlaySound(hoverSound);
                }
            }
        }
        // Cancel button
        Rectangle cancelBtn = new Rectangle(350, 400, 200, 45);
        bool cancelHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), cancelBtn);
        Raylib.DrawRectangleRec(cancelBtn, cancelHover ? Color.LightGray : Color.White);
        // Hover sound
        if (cancelHover && lastHoveredIndex != 202)
        {
            Raylib.PlaySound(hoverSound);
            lastHoveredIndex = 202;
        }
        else if (!cancelHover && lastHoveredIndex == 202)
        {
            lastHoveredIndex = -1;
        }
        Raylib.DrawRectangleLinesEx(cancelBtn, 1, Color.Gray);
        Raylib.DrawTextEx(fontRegular, "Cancel",
            new Vector2((int)(cancelBtn.X + (cancelBtn.Width - Raylib.MeasureText("Cancel", 24)) / 2),
            (int)(cancelBtn.Y + (cancelBtn.Height - 24) / 2)),
            24, 1, Color.Black);

        if (cancelHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Raylib.PlaySound(selectSound);
            ClearInputFields();
            subMenuOption = -1;
        }
        if (!string.IsNullOrEmpty(bookingErrorMessage))
        {
            if (Raylib.GetTime() - bookingErrorTime < 3.0)
            {
                Raylib.DrawTextEx(fontRegular, bookingErrorMessage, new Vector2(120, 470), 22, 1, Color.Red);
            }
            else
            {
                bookingErrorMessage = "";
            }
        }
    }
    private void DrawCheckOut()
    {
        Raylib.DrawRectangle(100, 180, 800, 500, Color.White);
        Raylib.DrawRectangleLines(100, 180, 800, 500, Color.Gray);
        Raylib.DrawTextEx(fontBold, "Check Out", new Vector2(120, 200), 30, 1, Color.DarkGray);

        DrawInputField("Room Number:", inputCheckoutRoom, 250, "checkoutRoom");

        // Submit button
        Rectangle submitBtn = new Rectangle(120, 320, 200, 45);
        bool submitHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), submitBtn);

        // Hover sound
        if (submitHover && lastHoveredIndex != 301)
        {
            Raylib.PlaySound(hoverSound);
            lastHoveredIndex = 301;
        }
        else if (!submitHover && lastHoveredIndex == 301)
        {
            lastHoveredIndex = -1;
        }
        Raylib.DrawRectangleRec(submitBtn, submitHover ? Color.LightGray : Color.White);
        Raylib.DrawRectangleLinesEx(submitBtn, 1, Color.Gray);
        Raylib.DrawTextEx(fontRegular, "Submit",
            new Vector2((int)(submitBtn.X + (submitBtn.Width - Raylib.MeasureText("Submit", 24)) / 2),
            (int)(submitBtn.Y + (submitBtn.Height - 24) / 2)),
            24, 1, Color.Black);
        if (submitHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Raylib.PlaySound(selectSound);
            if (int.TryParse(inputCheckoutRoom, out int room))
            {
                if (roomManager.CheckOut(room))
                {
                    ClearInputFields();
                    subMenuOption = -1;
                }
            }
        }
        // Cancel button
        Rectangle cancelBtn = new Rectangle(350, 320, 200, 45);
        bool cancelHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), cancelBtn);
        // Hover sound
        if (cancelHover && lastHoveredIndex != 302)
        {
            Raylib.PlaySound(hoverSound);
            lastHoveredIndex = 302;
        }
        else if (!cancelHover && lastHoveredIndex == 302)
        {
            lastHoveredIndex = -1;
        }
        Raylib.DrawRectangleRec(cancelBtn, cancelHover ? Color.LightGray : Color.White);
        Raylib.DrawRectangleLinesEx(cancelBtn, 1, Color.Gray);
        Raylib.DrawTextEx(fontRegular, "Cancel",
            new Vector2((int)(cancelBtn.X + (cancelBtn.Width - Raylib.MeasureText("Cancel", 24)) / 2),
            (int)(cancelBtn.Y + (cancelBtn.Height - 24) / 2)),
            24, 1, Color.Black);

        if (cancelHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Raylib.PlaySound(selectSound);
            ClearInputFields();
            subMenuOption = -1;
        }

        // Occupied rooms list
        Raylib.DrawTextEx(fontBold, "Occupied Rooms:", new Vector2(120, 380), 24, 1, Color.DarkGray);
        int y = 430;
        foreach (var room in roomManager.GetOccupiedRooms())
        {
            string customerName = "Unknown";
            if (room.CustomerId.HasValue)
            {
                var customer = customerManager.GetCustomerById(room.CustomerId.Value);
                if (customer != null)
                    customerName = customer.Name;
            }
            Raylib.DrawTextEx(fontBold, $"Room {room.Number} ({room.Type}) - {customerName}",
                new Vector2(140, y), 20, 1, Color.Black);
            y += 30;
        }
    }
    private void DrawBillingMenu()
    {
        Raylib.DrawRectangle(100, 180, 800, 500, Color.White);
        Raylib.DrawRectangleLines(100, 180, 800, 500, Color.Gray);
        Raylib.DrawTextEx(fontBold, "Billing Menu", new Vector2(120, 200), 30, 1, Color.DarkGray);

        inputBillingRoomNumber = DrawInputField("Room Number:", inputBillingRoomNumber, 260, "billingRoomNumber");

        Rectangle generateBtn = new Rectangle(120, 340, 200, 45);
        bool generateHover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), generateBtn);
        if (generateHover && lastHoveredIndex != 401)
        {
            Raylib.PlaySound(hoverSound);
            lastHoveredIndex = 401;
        }
        else if (!generateHover && lastHoveredIndex == 401)
        {
            lastHoveredIndex = -1;
        }
        Raylib.DrawRectangleRec(generateBtn, generateHover ? Color.LightGray : Color.White);
        Raylib.DrawRectangleLinesEx(generateBtn, 1, Color.Gray);
        Raylib.DrawTextEx(fontRegular, "Generate Bill",
            new Vector2((int)(generateBtn.X + 20), (int)(generateBtn.Y + 10)),
            24, 1, Color.Black);

        if (generateHover && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Raylib.PlaySound(selectSound);
            if (int.TryParse(inputBillingRoomNumber, out int roomNum))
            {
                var bill = billManager.GenerateBill(roomNum);
                if (bill != null)
                    generatedBill = bill;
            }
        }

        if (generatedBill != null)
        {
            Raylib.DrawTextEx(fontRegular, $"Customer: {generatedBill.CustomerName}", new Vector2(120, 420), 22, 1, Color.Black);
            Raylib.DrawTextEx(fontRegular, $"Room Type: {generatedBill.RoomType}", new Vector2(120, 450), 20, 1, Color.DarkGray);
            Raylib.DrawTextEx(fontRegular, $"Total Amount: Rs. {generatedBill.TotalAmount}", new Vector2(120, 480), 20, 1, Color.DarkGray);
        }
    }
    private void DrawReports()
    {
        Raylib.DrawRectangle(100, 180, 800, 400, Color.White);
        Raylib.DrawRectangleLines(100, 180, 800, 400, Color.Gray);
        Raylib.DrawTextEx(fontRegular, "Reports Dashboard", new Vector2(120, 200), 30, 1, Color.DarkGray);

        Raylib.DrawTextEx(fontRegular, "Occupancy Rate: 75%", new Vector2(140, 250), 24, 1, Color.Black);
        Raylib.DrawTextEx(fontRegular, "Revenue: $12,450", new Vector2(140, 300), 24, 1, Color.Black);
        Raylib.DrawTextEx(fontRegular, "Most Popular: Double", new Vector2(140, 350), 24, 1, Color.Black);
    }

    private void DrawCredits()
    {
        Raylib.DrawRectangle(100, 180, 800, 400, Color.White);
        Raylib.DrawRectangleLines(100, 180, 800, 400, Color.Gray);
        Raylib.DrawTextEx(fontBold, "Development Team", new Vector2(120, 200), 30, 1, Color.DarkGray);

        int y = 250;
        foreach (var (name, role) in credits)
        {
            Raylib.DrawTextEx(fontRegular, name, new Vector2(150, y), 26, 1, Color.Black);
            Raylib.DrawTextEx(fontRegular, role, new Vector2(500, y), 26, 1, Color.DarkGray);
            y += 50;
        }
    }
    private string DrawInputField(string label, string value, int yPos, string fieldName)
    {
        Raylib.DrawTextEx(fontRegular, label, new Vector2(120, yPos), 24, 1, Color.Black);
        Rectangle inputRect = new Rectangle(350, yPos, 400, 35);

        bool isFieldActive = isInputActive && activeInputField == fieldName;
        bool isHovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), inputRect);

        // Change color if hovered or active
        Color bgColor = isFieldActive ? Color.LightGray : (isHovered ? new Color(240, 240, 240, 255) : Color.White);
        Raylib.DrawRectangleRec(inputRect, bgColor);
        Raylib.DrawRectangleLinesEx(inputRect, 1, isFieldActive ? Color.Blue : Color.LightGray);

        Raylib.DrawTextEx(fontRegular, value, new Vector2((int)inputRect.X + 10, (int)inputRect.Y + 5), 24, 1, Color.Black);

        // Handle mouse click to activate input
        if (isHovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            isInputActive = true;
            activeInputField = fieldName;
        }
        // Draw cursor if this field is active
        if (isFieldActive && showCursor)
        {
            float cursorX = inputRect.X + 10 + Raylib.MeasureTextEx(fontRegular, value, 24, 1).X;
            Raylib.DrawRectangle((int)cursorX, (int)inputRect.Y + 5, 2, 25, Color.Black);
        }
        return value;
    }
    private void HandleTextInput()
    {
        int ch = Raylib.GetCharPressed();
        while (ch > 0)
        {
            if (ch >= 32 && ch <= 126)
            {
                string fieldValue = GetActiveInputField();
                fieldValue += (char)ch;
                SetActiveInputField(fieldValue);
            }
            ch = Raylib.GetCharPressed();
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
        {
            string fieldValue = GetActiveInputField();
            if (fieldValue.Length > 0)
            {
                fieldValue = fieldValue.Substring(0, fieldValue.Length - 1);
                SetActiveInputField(fieldValue);
            }
        }
        // Clicking outside the input field or pressing Enter deactivates it
        if ((Raylib.IsMouseButtonPressed(MouseButton.Left) &&
             !Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), new Rectangle(350, 0, 400, 768))) ||
            Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            isInputActive = false;
            activeInputField = "";
        }
    }
    private string GetActiveInputField()
    {
        return activeInputField switch
        {
            "name" => inputName,
            "contact" => inputContact,
            "email" => inputEmail,
            "roomNumber" => inputRoomNumber,
            "customerId" => inputCustomerId,
            "checkoutRoom" => inputCheckoutRoom,
            "billingRoomNumber" => inputBillingRoomNumber,
            _ => ""
        };
    }

    private void SetActiveInputField(string value)
    {
        switch (activeInputField)
        {
            case "name": 
                inputName = value;
                break;
            case "contact":
                inputContact = value;
                break;
            case "email":
                inputEmail = value;
                break;
            case "roomNumber":
                inputRoomNumber = value;
                break;
            case "customerId":
                inputCustomerId = value;
                break;
            case "checkoutRoom":
                inputCheckoutRoom = value;
                break;
            case "billingRoomNumber":
                inputBillingRoomNumber = value;
                break;
        }
    }
    private List<string> GetCurrentOptions()
    {
        return currentScreen switch
        {
            0 => mainMenuOptions,
            1 => customerMenuOptions,
            2 => roomMenuOptions,
            _ => new List<string>()
        };
    }

    private void ClearInputFields()
    {
        inputName = inputContact = inputEmail = "";
        inputRoomNumber = inputCustomerId = inputCheckoutRoom = "";
        isInputActive = false;
        activeInputField = "";
    }
}
public class Program
{
    public static void Main()
    {
        new HotelSystem().Run();
    }
}