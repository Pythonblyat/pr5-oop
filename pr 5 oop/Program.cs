using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace PraktEventsObserver
{
    internal static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Demo1_1(); Demo1_2(); Demo2_1(); Demo3_1();
        }

        // ЗАВДАННЯ 1.1: CountdownTimer (екземплярний, статичний, лямбда, відписка -=)
        private static void Demo1_1()
        {
            Console.WriteLine("=== ЗАВДАННЯ 1.1: Таймер зворотного відліку ===");
            var timer = new CountdownTimer();
            var display = new ConsoleDisplay();
            var alert = new SoundAlert();
            Action<int> lambdaHandler = sec => Console.WriteLine($"  [Лямбда] {sec} сек.");

            timer.Tick += display.OnTick;
            timer.Tick += StaticTickLogger;
            timer.Tick += lambdaHandler;
            timer.Finished += alert.OnFinished;
            timer.Start(3);

            Console.WriteLine("--- Після відписки лямбди та статичного методу ---");
            timer.Tick -= lambdaHandler;
            timer.Tick -= StaticTickLogger;
            timer.Start(2);
            Console.WriteLine();
        }
        private static void StaticTickLogger(int sec) => Console.WriteLine($"  [Статичний метод] лог тіку: {sec}");

        // ЗАВДАННЯ 1.2: TemperatureSensor (EventHandler<TemperatureEventArgs>)
        private static void Demo1_2()
        {
            Console.WriteLine("=== ЗАВДАННЯ 1.2: Моніторинг температури ===");
            var sensor = new TemperatureSensor();
            var display = new DisplayPanel();
            var alarm = new AlarmSystem(criticalHigh: 38.0);
            var logger = new DataLogger();

            sensor.TemperatureChanged += display.OnTemperatureChanged;
            sensor.TemperatureChanged += alarm.OnTemperatureChanged;
            sensor.TemperatureChanged += logger.OnTemperatureChanged;

            sensor.CurrentTemperature = 22.5;
            sensor.CurrentTemperature = 35.0;
            sensor.CurrentTemperature = 42.0;
            sensor.CurrentTemperature = 15.0;
            Console.WriteLine();
        }

        // ЗАВДАННЯ 2.1: Pub-Sub система замовлень (OrderManager)
        private static void Demo2_1()
        {
            Console.WriteLine("=== ЗАВДАННЯ 2.1: Pub-Sub система замовлень ===");
            var manager = new OrderManager();
            var email = new EmailNotifier();
            var inventory = new InventoryManager();
            var accounting = new AccountingService();

            manager.OrderCreated += email.OnOrderCreated;
            manager.OrderCreated += inventory.OnOrderCreated;
            manager.OrderPaid += email.OnOrderPaid;
            manager.OrderPaid += accounting.OnOrderPaid;
            manager.OrderCancelled += email.OnOrderCancelled;
            manager.OrderCancelled += inventory.OnOrderCancelled;
            manager.OrderCancelled += accounting.OnOrderCancelled;

            manager.CreateOrder("Ноутбук", 25000m, 2);
            manager.PayOrder(1, "Ноутбук", 25000m, 2);
            manager.CreateOrder("Телефон", 15000m, 1);
            manager.CancelOrder(2, "Телефон", 15000m, 1);
            inventory.PrintStock(); accounting.PrintBalance();
            Console.WriteLine();
        }

        // ЗАВДАННЯ 3.1: Smart Home (Observer через події + динамічна підписка/відписка)
        private static void Demo3_1()
        {
            Console.WriteLine("=== ЗАВДАННЯ 3.1: Розумний дім (Observer) ===");
            var sensor = new MotionSensor();
            var light = new SmartLight("Коридор");
            var camera = new SecurityCamera();
            var alarm = new AlarmPanel();
            var doorLock = new SmartDoorLock();

            sensor.MotionDetected += light.OnMotionDetected;
            sensor.MotionDetected += camera.OnMotionDetected;
            sensor.MotionDetected += alarm.OnMotionDetected;
            sensor.MotionDetected += doorLock.OnMotionDetected;

            sensor.DetectMotion("Коридор", ActivityLevel.Low);
            sensor.DetectMotion("Вхідні двері", ActivityLevel.High);

            Console.WriteLine("\n--- Відключення тривоги ---");
            sensor.MotionDetected -= alarm.OnMotionDetected;
            sensor.DetectMotion("Кухня", ActivityLevel.Medium);

            Console.WriteLine("\n--- Підключення тривоги ---");
            sensor.MotionDetected += alarm.OnMotionDetected;
            sensor.DetectMotion("Гараж", ActivityLevel.High);
        }
    }

    // --- Завдання 1.1 ---
    public class CountdownTimer
    {
        public event Action<int>? Tick;
        public event Action? Finished;
        public void Start(int seconds) { for (int i = seconds; i >= 0; i--) { Tick?.Invoke(i); Thread.Sleep(250); } Finished?.Invoke(); }
    }
    public class ConsoleDisplay { public void OnTick(int secondsLeft) => Console.WriteLine($"[Дисплей] Залишилось: {secondsLeft} с"); }
    public class SoundAlert { public void OnFinished() => Console.WriteLine("[Звук] ДЗИНЬ! Час вичерпано!"); }

    // --- Завдання 1.2 ---
    public class TemperatureEventArgs : EventArgs
    {
        public double OldTemperature { get; }
        public double NewTemperature { get; }
        public string Unit { get; }
        public DateTime Timestamp { get; }
        public double Difference => NewTemperature - OldTemperature;
        public TemperatureEventArgs(double oldTemp, double newTemp, string unit = "°C") { OldTemperature = oldTemp; NewTemperature = newTemp; Unit = unit; Timestamp = DateTime.Now; }
    }
    public class TemperatureSensor
    {
        private double _currentTemp;
        public event EventHandler<TemperatureEventArgs>? TemperatureChanged;
        public double CurrentTemperature { get => _currentTemp; set { if (Math.Abs(_currentTemp - value) > 0.01) { double old = _currentTemp; _currentTemp = value; OnTemperatureChanged(new TemperatureEventArgs(old, value)); } } }
        protected virtual void OnTemperatureChanged(TemperatureEventArgs e) => TemperatureChanged?.Invoke(this, e);
    }
    public class DisplayPanel { public void OnTemperatureChanged(object? s, TemperatureEventArgs e) => Console.WriteLine($"[Дисплей] {e.NewTemperature:F1}{e.Unit} {(e.Difference > 0 ? "↑" : "↓")} (зміна: {e.Difference:+0.0;-0.0}{e.Unit})"); }
    public class AlarmSystem
    {
        private readonly double _high, _low;
        public AlarmSystem(double criticalHigh = 40.0, double criticalLow = -10.0) { _high = criticalHigh; _low = criticalLow; }
        public void OnTemperatureChanged(object? s, TemperatureEventArgs e) { if (e.NewTemperature > _high || e.NewTemperature < _low) Console.WriteLine($"[ТРИВОГА] Критична температура: {e.NewTemperature:F1}{e.Unit}!"); }
    }
    public class DataLogger { public void OnTemperatureChanged(object? s, TemperatureEventArgs e) => Console.WriteLine($"[Лог {e.Timestamp:HH:mm:ss}] {e.OldTemperature:F1} -> {e.NewTemperature:F1}{e.Unit}"); }

    // --- Завдання 2.1 ---
    public class OrderEventArgs : EventArgs
    {
        public int OrderId { get; }
        public string ProductName { get; }
        public decimal Price { get; }
        public int Quantity { get; }
        public DateTime OrderTime { get; }
        public decimal TotalPrice => Price * Quantity;
        public OrderEventArgs(int id, string name, decimal price, int qty) { OrderId = id; ProductName = name; Price = price; Quantity = qty; OrderTime = DateTime.Now; }
    }
    public class OrderManager
    {
        private int _nextId = 1;
        public event EventHandler<OrderEventArgs>? OrderCreated, OrderPaid, OrderCancelled;
        public void CreateOrder(string p, decimal pr, int q) { var a = new OrderEventArgs(_nextId++, p, pr, q); Console.WriteLine($"\n--- Створення замовлення #{a.OrderId}: {p} x{q} ---"); OnOrderCreated(a); }
        public void PayOrder(int id, string p, decimal pr, int q) { Console.WriteLine($"\n--- Оплата замовлення #{id} ---"); OnOrderPaid(new OrderEventArgs(id, p, pr, q)); }
        public void CancelOrder(int id, string p, decimal pr, int q) { Console.WriteLine($"\n--- Скасування замовлення #{id} ---"); OnOrderCancelled(new OrderEventArgs(id, p, pr, q)); }
        protected virtual void OnOrderCreated(OrderEventArgs e) => OrderCreated?.Invoke(this, e);
        protected virtual void OnOrderPaid(OrderEventArgs e) => OrderPaid?.Invoke(this, e);
        protected virtual void OnOrderCancelled(OrderEventArgs e) => OrderCancelled?.Invoke(this, e);
    }
    public class EmailNotifier
    {
        public void OnOrderCreated(object? s, OrderEventArgs e) => Console.WriteLine($"  [Email] Замовлення #{e.OrderId} ({e.ProductName} x{e.Quantity}) створено.");
        public void OnOrderPaid(object? s, OrderEventArgs e) => Console.WriteLine($"  [Email] Оплата {e.TotalPrice:F2} грн за замовлення #{e.OrderId} отримана.");
        public void OnOrderCancelled(object? s, OrderEventArgs e) => Console.WriteLine($"  [Email] Замовлення #{e.OrderId} скасовано.");
    }
    public class InventoryManager
    {
        private readonly Dictionary<string, int> _stock = new() { { "Ноутбук", 50 }, { "Телефон", 100 }, { "Навушники", 200 } };
        public void OnOrderCreated(object? s, OrderEventArgs e) { if (_stock.ContainsKey(e.ProductName)) _stock[e.ProductName] -= e.Quantity; Console.WriteLine($"  [Склад] Зарезервовано {e.ProductName}: -{e.Quantity} шт."); }
        public void OnOrderCancelled(object? s, OrderEventArgs e) { if (_stock.ContainsKey(e.ProductName)) _stock[e.ProductName] += e.Quantity; Console.WriteLine($"  [Склад] Повернено {e.ProductName}: +{e.Quantity} шт."); }
        public void PrintStock() { Console.WriteLine("\n  Стан складу:"); foreach (var item in _stock) Console.WriteLine($"    {item.Key}: {item.Value} шт."); }
    }
    public class AccountingService
    {
        private decimal _totalRevenue; private readonly HashSet<int> _paidOrders = new();
        public void OnOrderPaid(object? s, OrderEventArgs e) { _paidOrders.Add(e.OrderId); _totalRevenue += e.TotalPrice; Console.WriteLine($"  [Бухгалтерія] Нараховано +{e.TotalPrice:F2} грн"); }
        public void OnOrderCancelled(object? s, OrderEventArgs e) { if (_paidOrders.Remove(e.OrderId)) { _totalRevenue -= e.TotalPrice; Console.WriteLine($"  [Бухгалтерія] Повернення коштів -{e.TotalPrice:F2} грн"); } }
        public void PrintBalance() => Console.WriteLine($"\n  Загальний дохід: {_totalRevenue:F2} грн");
    }

    // --- Завдання 3.1 ---
    public enum ActivityLevel { Low, Medium, High }
    public class MotionEventArgs : EventArgs { public string Zone { get; } public ActivityLevel Level { get; } public DateTime DetectedAt { get; } public MotionEventArgs(string z, ActivityLevel l) { Zone = z; Level = l; DetectedAt = DateTime.Now; } }
    public class MotionSensor
    {
        public event EventHandler<MotionEventArgs>? MotionDetected;
        public void DetectMotion(string zone, ActivityLevel level) { Console.WriteLine($"\n=== Рух виявлено: зона «{zone}», рівень: {level} ==="); OnMotionDetected(new MotionEventArgs(zone, level)); }
        protected virtual void OnMotionDetected(MotionEventArgs e) => MotionDetected?.Invoke(this, e);
    }
    public class SmartLight { private readonly string _loc; public SmartLight(string loc) => _loc = loc; public void OnMotionDetected(object? s, MotionEventArgs e) => Console.WriteLine($"  [SmartLight ({_loc})] Світло увімкнено у зоні «{e.Zone}»"); }
    public class SecurityCamera { public void OnMotionDetected(object? s, MotionEventArgs e) { if (e.Level >= ActivityLevel.Medium) Console.WriteLine($"  [Camera] Запис активовано у зоні «{e.Zone}» ({e.DetectedAt:HH:mm:ss})"); } }
    public class AlarmPanel { public void OnMotionDetected(object? s, MotionEventArgs e) { if (e.Level == ActivityLevel.High) Console.WriteLine($"  [AlarmPanel] СИРЕНА! Висока активність у зоні «{e.Zone}»!"); } }
    public class SmartDoorLock { public void OnMotionDetected(object? s, MotionEventArgs e) { if (e.Level == ActivityLevel.High) Console.WriteLine($"  [DoorLock] Двері заблоковано через тривогу в «{e.Zone}»!"); } }
}