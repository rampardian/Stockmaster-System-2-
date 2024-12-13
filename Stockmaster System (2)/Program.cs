using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Data;
using Spectre.Console;
using System.Globalization;
using System.Xml.Serialization;
using System.Runtime.Remoting.Metadata;
using System.Diagnostics.Eventing.Reader;
using System.Collections;

// Jared Rey A. Adlawan

namespace Stockmaster_System
{
    interface IOwnerOperations
    {
        void AddProduct(string name, decimal price, int stockLevel, string qualityCheck);
        void DeleteProduct(string name);
        void EditProductDetails(string name, decimal price, int stockLevel, string qualityCheck);
        void DisplayProducts();
        void SetLowThreshold();
        void SetHighThreshold();

    }

    interface ITellerOperations
    {
        void Order();
        void SearchProduct(string productName);
    }

    abstract class BaseProduct
    {
        public abstract string Name { get; set; }
        public abstract decimal Price { get; set; }
        public abstract int StockLevel { get; set; }
        public abstract string QualityCheck { get; set; }
    }

    class Product : BaseProduct
    {
        public override string Name { get; set; }
        public override decimal Price { get; set; }
        public override int StockLevel { get; set; }
        public override string QualityCheck { get; set; }

    }

    class OrderProduct
    {
        public string OrderName { get; set; }
        public int OrderQuantity { get; set; }
        public decimal OrderPrice { get; set; }
        public decimal totalProductPrice { get; set; }
    }

    class Owner : IOwnerOperations
    {
        private static List<Product> products = new List<Product>();
        private ProductsSave productsSave = new ProductsSave();
        private StockChecker stockChecker; 

        public Owner()
        {
            products = productsSave.LoadProducts();
            stockChecker = new StockChecker(10, 50);
        }

        public List<Product> Products
        {
            get
            {
                return products;
            }
            set
            {
                products = value;
            }
        }

        public int LowThreshold => stockChecker.LowThreshold; // getter LowThreshold
        public int HighThreshold => stockChecker.HighThreshold; // getter HighThreshold

        public void AddProduct(string name, decimal price, int stockLevel, string qualityCheck)
        {
            products.Add(new Product
            {
                Name = name,
                Price = price,
                StockLevel = stockLevel,
                QualityCheck = qualityCheck
            });
            Console.WriteLine("Product added successfully.");
            stockChecker.CheckStock(products.Last()); // Check stock level after adding
            productsSave.SaveProducts(products); // Save products after adding
            Thread.Sleep(1500);
        }

        public void DeleteProduct(string name)
        {
            Product NullProduct = null;
            foreach (var product in products)
            {
                if (product.Name == name)
                {
                    NullProduct = product;
                    break;
                }
            }
            if (NullProduct != null)
            {
                products.Remove(NullProduct);
                Console.WriteLine("Product deleted successfully.");
                productsSave.SaveProducts(products); // Save products after deleting
                Thread.Sleep(1500);
            }
            else
            {
                Console.WriteLine("Product not found.");
                Thread.Sleep(1500);
            }
        }

        public void EditProductDetails(string name, decimal price, int stockLevel, string qualityCheck)
        {
            Product EditedProduct = null;
            foreach (var product in products)
            {
                if (product.Name == name)
                {
                    EditedProduct = product;
                    break;
                }
            }
            if (EditedProduct != null)
            {
                EditedProduct.Price = price;
                EditedProduct.StockLevel = stockLevel;
                EditedProduct.QualityCheck = qualityCheck;
                Console.WriteLine("Product details updated.");
                stockChecker.CheckStock(EditedProduct); // Check stock level after editing
                productsSave.SaveProducts(products); // Save products after editing
                Thread.Sleep(1500);
            }
            else
            {
                Console.WriteLine("Product not found.");
                Thread.Sleep(1500);
            }
        }

        public void DisplayProducts()
        {
            var Displaytable = new Table();

            Displaytable.AddColumn("Name");
            Displaytable.AddColumn("Price");
            Displaytable.AddColumn("Stock Level");
            Displaytable.AddColumn("Quality");

            var cultureInfo = new CultureInfo("en-PH");

            foreach (var product in products)
            {
                string priceDisplay = "₱" + product.Price.ToString("N2", cultureInfo); 
                Displaytable.AddRow(product.Name, priceDisplay, product.StockLevel.ToString(), product.QualityCheck);
            }
            AnsiConsole.Render(Displaytable);

            AnsiConsole.MarkupLine("[grey]-----------------Nothing Follows-----------------[/]");
        }

        public void SetLowThreshold()
        {
            Console.Write("Enter new low threshold: ");
            if (int.TryParse(Console.ReadLine(), out int newLowThreshold))
            {
                stockChecker.SetLowThreshold(newLowThreshold);
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a valid number.");
            }
            Thread.Sleep(1500);
        }

        public void SetHighThreshold()
        {
            Console.Write("Enter new high stock threshold: ");
            if (int.TryParse(Console.ReadLine(), out int newHighThreshold))
            {
                stockChecker.SetHighThreshold(newHighThreshold);
            }
            else
            {
                Console.WriteLine("Invalid input. Please enter a valid number.");
            }
            Thread.Sleep(1500);
        }

        public void CheckAllStockLevels()
        {
            Console.WriteLine("Checking stock levels for all products...");
            foreach (var product in products)
            {
                stockChecker.CheckStock(product);
            }
        }
    }

    class Teller : ITellerOperations
    {
        private List<Product> products;
        private static List<OrderProduct> OrderedProductDetails = new List<OrderProduct>();
        private List<decimal> ProductPrice = new List<decimal>();
        ProductsSave productsSave = new ProductsSave();

        public Teller(List<Product> products)
        {
            this.products = products;
        }

        public void Order()
        {
            decimal TransTotal = 0;
            while (true)
            {
                Console.Write("Enter Product Name: ");
                string productName = Console.ReadLine();
                bool check = false;

                for (int i = 0; i < products.Count; i++)
                {
                    if (products[i].Name == productName)
                    {
                        check = true;
                        Console.WriteLine("Product selected: {0} | Stock Level: {1}", products[i].Name, products[i].StockLevel);
                        Console.Write("Enter Quantity: ");

                        // Safe parsing for quantity
                        if (!int.TryParse(Console.ReadLine(), out int quantity) || quantity <= 0)
                        {
                            Console.WriteLine("Invalid order quantity! Input should be a positive integer.");
                            return;
                        }

                        if (quantity > products[i].StockLevel)
                        {
                            Console.WriteLine("Insufficient stock! Input should be lesser than the current stock level.");
                        }
                        else
                        {
                            decimal totalProductPrice = products[i].Price * quantity;
                            products[i].StockLevel -= quantity; // Update new stock level

                            OrderedProductDetails.Add(new OrderProduct
                            {
                                OrderName = products[i].Name,
                                OrderQuantity = quantity,
                                OrderPrice = products[i].Price,
                                totalProductPrice = totalProductPrice
                            });
                            TransTotal += totalProductPrice; // Add to transaction total
                            
                            // Save updated product list after the order
                            ProductsSave productsSave = new ProductsSave();
                            productsSave.SaveProducts(products); 
                            break; // Exit the product loop after a successful order
                        }
                    }
                }
                if (!check)
                {
                    Console.WriteLine("Product not found!");
                }

                Console.WriteLine("Add another product? [y/n]");
                string orderOpt = Console.ReadLine().ToLower();
                if (orderOpt != "y")
                {
                    break;
                }
            }

            Console.Clear();
            var Ordertable = new Table();
            Console.WriteLine("Transaction: ");
            Ordertable.AddColumn("Name");
            Ordertable.AddColumn("Price");
            Ordertable.AddColumn("Quantity");
            Ordertable.AddColumn("Total Price");

            var cultureInfo = new CultureInfo("en-PH");

            foreach (var product in OrderedProductDetails)
            {
                string priceDisplay = "₱" + product.OrderPrice.ToString("N2", cultureInfo);
                string totalPriceDisplay = "₱" + product.totalProductPrice.ToString("N2", cultureInfo);
                Ordertable.AddRow(product.OrderName, priceDisplay, product.OrderQuantity.ToString(), totalPriceDisplay);
            }
            AnsiConsole.Render(Ordertable);

            AnsiConsole.MarkupLine("[grey]-----------------Nothing Follows-----------------[/]");

            Console.WriteLine("Transaction Total: ₱{0}", TransTotal.ToString("N2", cultureInfo));

            //Payment proces
            decimal payment = 0;
            while (true)
            {
                Console.Write("Enter Payment Amount: ");
                if (!decimal.TryParse(Console.ReadLine(), out payment) || payment <= 0)
                {
                    Console.WriteLine("Invalid payment amount! Please enter a positive number.");
                    continue; // Ask for payment again
                }

                if (payment < TransTotal)
                {
                    Console.WriteLine("Insufficient payment! Please enter an amount greater than or equal to the transaction total.");
                }
                else
                {
                    break; // Valid payment
                }
            }

            decimal change = payment - TransTotal;
            Console.WriteLine("Payment accepted. Change: ₱{0}", change.ToString("N2", cultureInfo));
            Thread.Sleep(1500);

            //Print receipt
            Console.WriteLine("\nReceipt:");

            //Get current date and time
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine("Transaction Date and Time: {0}", dateTime);

            foreach (var product in OrderedProductDetails)
            {
                Console.WriteLine("{0} || Qty: {1} || Total: ₱{2}", product.OrderName, product.OrderQuantity, product.totalProductPrice.ToString("N2", cultureInfo));
            }
            Console.WriteLine("Transaction Total: ₱{0}", TransTotal.ToString("N2", cultureInfo));
            Console.WriteLine("Payment: ₱{0}", payment.ToString("N2", cultureInfo));
            Console.WriteLine("Change: ₱{0}", change.ToString("N2", cultureInfo));
            Console.WriteLine("Thank you for your purchase!");

            TransactionSave receipt = new TransactionSave();
            receipt.SaveTransactionTotal(TransTotal);

            Console.ReadLine();
        }

        public void SearchProduct(string productName)
        {
            Product product = null;
            foreach (var p in products) // Use the products list directly
            {
                if (p.Name.ToLower() == productName.ToLower())
                {
                    product = p;
                    break;
                }
            }
            if (product != null)
            {
                Console.WriteLine("Product Found: {0}", product.Name);
                Console.WriteLine("Price: {0}", product.Price);
                Console.WriteLine("Stock Level: {0}", product.StockLevel);
                Console.WriteLine("Quality: {0}", product.QualityCheck);
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Product not found.");
                Thread.Sleep(1500);
            }
        }
    }



    class TransactionSave
    {
        private const string ReceiptFilePath = @"C:\Users\Jared Adlawan\source\repos\Stockmaster System\TransactionReceipts\Receipts.txt";

        public void SaveTransactionTotal(decimal total)
        {
            using (StreamWriter writer = new StreamWriter(ReceiptFilePath, true))
            {
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{total}");
            }
        }
    }

    public class SalesReportGenerator
    {
        private const string ReceiptFilePath = @"C:\Users\Jared Adlawan\source\repos\Stockmaster System\TransactionReceipts\Receipts.txt";

        public void GenerateSalesReport(DateTime startDate, DateTime endDate)
        {
            decimal totalSales = GetSalesTotal(startDate, endDate);
            Console.WriteLine($"Total sales from {startDate.ToShortDateString()} to {endDate.ToShortDateString()}: {totalSales:C}");
            Console.ReadLine();
        }

        private decimal GetSalesTotal(DateTime startDate, DateTime endDate)
        {
            decimal totalSales = 0;

            if (!File.Exists(ReceiptFilePath))
            {
                Console.WriteLine("No transaction receipts found.");
                Console.ReadLine();
                return totalSales;
            }

            using (StreamReader reader = new StreamReader(ReceiptFilePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(',');
                    DateTime transactionDate = DateTime.Parse(parts[0]);
                    decimal transactionTotal = decimal.Parse(parts[1]);

                    if (transactionDate >= startDate && transactionDate <= endDate)
                    {
                        totalSales += transactionTotal;
                    }
                }
            }

            return totalSales;
        }
    }

    public class SalesReportHandler
    {
        private readonly SalesReportGenerator _reportGenerator;

        public SalesReportHandler(SalesReportGenerator reportGenerator)
        {
            _reportGenerator = reportGenerator;
        }

        public void GenerateSalesReport()
        {
            Console.Write("Enter the start date for the report (yyyy-MM-dd): ");
            DateTime startDate;
            while (!DateTime.TryParse(Console.ReadLine(), out startDate))
            {
                Console.WriteLine("Invalid date format. Please try again.");
            }

            Console.WriteLine("Select report type: ");
            Console.WriteLine("1. Weekly");
            Console.WriteLine("2. Monthly");
            Console.WriteLine("3. Yearly");
            int reportType = int.Parse(Console.ReadLine());

            DateTime endDate = DateTime.Now; // Default to now
            switch (reportType)
            {
                case 1:
                    endDate = startDate.AddDays(6);
                    break;
                case 2:
                    endDate = startDate.AddDays(29);
                    break;
                case 3:
                    endDate = startDate.AddDays(364);
                    break;
                default:
                    Console.WriteLine("Invalid report type.");
                    return;
            }

            _reportGenerator.GenerateSalesReport(startDate, endDate);
        }
    }

    class StockChecker
    {
        private int _lowThreshold;
        private int _highThreshold;

        public int LowThreshold
        {
            get
            {
                return _lowThreshold;
            }
        }

        public int HighThreshold 
        {
            get
            {
                return _highThreshold;
            }
        }

        public StockChecker(int lowThreshold, int highThreshold)
        {
            _lowThreshold = lowThreshold;
            _highThreshold = highThreshold;
        }

        public void CheckStock(Product product)
        {
            if (product.StockLevel < _lowThreshold)
            {
                Console.WriteLine($"[Shortage WARNING]  {product.Name} has low stock level: {product.StockLevel}.");
            }
            else if (product.StockLevel > _highThreshold)
            {
                Console.WriteLine($"[Surplus WARNING]  {product.Name} has excess stock level: {product.StockLevel}.");
            }
        }

        public void SetLowThreshold(int lowThreshold)
        {
            _lowThreshold = lowThreshold;
            Console.WriteLine("Low stock threshold set to: {0}", _lowThreshold);
        }

        public void SetHighThreshold(int highThreshold)
        {
            _highThreshold = highThreshold;
            Console.WriteLine("High stock threshold set to: {0}", _highThreshold);
        }
    }
    class LoginPage
    {
        private UsersSave userssave = new UsersSave();
        private string ownerPassword;
        private string tellerPassword; 
        private const string secretPasscode = "secretpass";

        public LoginPage()
        {
            (ownerPassword, tellerPassword) = userssave.LoadCredentials(); 
        }


        private string ReadPassword()
        {
            StringBuilder password = new StringBuilder();
            ConsoleKeyInfo key;

            while (true)
            {
                key = Console.ReadKey(true); // Read key

                if (key.Key == ConsoleKey.Enter) // check if Enter key is pressed
                {
                    Console.WriteLine(); // Move to next line
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace) // check If Backspace key is pressed
                {
                    if (password.Length > 0) // Check if there is something to delete
                    {
                        password.Remove(password.Length - 1, 1); // Remove last character
                        Console.Write("\b \b"); // Move back, print space, and move back again
                    }
                }
                else // Any other key
                {
                    password.Append(key.KeyChar); // Add character to password
                    Console.Write("*");
                }
            }

            return password.ToString(); 
        }

        public void Logo()
        {
            Console.Clear();
            Console.WriteLine("###### ######## ######## ####### ##  # ##     ##   ###   ###### ######## ###### ######");
            Console.WriteLine("##        ##    ##    ## ##      ## #  ###   ### ##   ## ##        ##    ##     ##   ##");
            Console.WriteLine("######    ##    ##    ## ##      ###   ## # # ## ####### ######    ##    ###### ######");
            Console.WriteLine("    ##    ##    ##    ## ##      ## #  ##  #  ## ##   ##     ##    ##    ##     ##   #");
            Console.WriteLine("######    ##    ######## ####### ##  # ##     ## ##   ## ######    ##    ###### ##   ##");
        }

        public string DisplayLogin()
        {
            Logo();
            while (true)
            {
                Console.Write("Enter role (Owner/Teller): ");
                string role = Console.ReadLine().ToLower();
                if (role == "owner" || role == "teller" || role == "secret")
                {
                    Console.Write("Enter password: ");
                    string password = ReadPassword(); 

                    if (role == "owner" && password == ownerPassword)
                    {
                        return "owner";
                    }
                    if (role == "teller" && password == tellerPassword)
                    {
                        return "teller";
                    }
                    if (role == "secret" && password == secretPasscode)
                    {
                        return "secret";
                    }
                    else
                    {
                        Console.WriteLine("Invalid Login.");
                        Thread.Sleep(1500);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid role input! ");
                    Thread.Sleep(1500);
                    continue;
                }
            }
        }

        public void ChangePassword()
        {
            do
            {
                Logo();
                Console.WriteLine("Which account password to change? [owner/teller] ");
                string changepass = Console.ReadLine().ToLower();

                if (changepass == "owner")
                {
                    Console.Clear();
                    Console.Write("Enter new password for Owner: ");
                    ownerPassword = Console.ReadLine();
                    userssave.SaveCredentials(ownerPassword, tellerPassword); // Save updated owner password
                    Console.WriteLine("Password updated successfully.");
                    Thread.Sleep(1500);
                    break;
                }
                else if (changepass == "teller")
                {
                    Console.Clear();
                    Console.Write("Enter new password for Teller: ");
                    tellerPassword = Console.ReadLine(); // Update teller password
                    userssave.SaveCredentials(ownerPassword, tellerPassword); // Save updated passwords
                    Console.WriteLine("Teller password updated successfully.");
                    Thread.Sleep(1500);
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid account name! ");
                    Thread.Sleep(1500);
                }
            } while (true);
        }
    }

    class UsersSave
    {
        private const string FilePath = @"C:\Users\Jared Adlawan\source\repos\Stockmaster System\UserCredentials\UserCredentials.txt";

        public (string ownerPassword, string tellerPassword) LoadCredentials()
        {
            string ownerPassword = null;
            string tellerPassword = null;

            if (!File.Exists(FilePath))
            {
                Console.WriteLine("No saved user credentials found");
                Thread.Sleep(1500);
                return (ownerPassword, tellerPassword);
            }

            using (StreamReader reader = new StreamReader(FilePath))
            {
                ownerPassword = reader.ReadLine();
                tellerPassword = reader.ReadLine();
            }

            return (ownerPassword, tellerPassword);
        }

        public void SaveCredentials(string ownerPassword, string tellerPassword)
        {
            using (StreamWriter writer = new StreamWriter(FilePath))
            {
                writer.WriteLine(ownerPassword); // Save owner password
                writer.WriteLine(tellerPassword); // Save teller password
            }
            Console.WriteLine("Credentials saved successfully.");
            Thread.Sleep(1500);
        }
    }

    class ProductsSave
    {
        private const string FilePath = @"C:\Users\Jared Adlawan\source\repos\Stockmaster System\ProductList\ProductList.txt";

        public void SaveProducts(List<Product> products) 
        {
            using (StreamWriter writer = new StreamWriter(FilePath))
            {
                foreach (var product in products)
                {
                    writer.WriteLine("{0},{1},{2},{3}", product.Name, product.Price, product.StockLevel, product.QualityCheck);
                }
            }

            Console.WriteLine("Products saved successfully.");

            Thread.Sleep(1500);
        }

        public List<Product> LoadProducts()
        {
            var products = new List<Product>();

            if (!File.Exists(FilePath))
            {
                Console.WriteLine("No saved products found.");
                Thread.Sleep(1500);
                return products; // Return the empty product list
            }

            using (StreamReader reader = new StreamReader(FilePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');

                    if (parts.Length == 4)
                    {
                        // Create a new Product object and populate its properties from the parts
                        products.Add(new Product
                        {
                            Name = parts[0],
                            Price = decimal.Parse(parts[1]), 
                            StockLevel = int.Parse(parts[2]), 
                            QualityCheck = parts[3] 
                        });
                    }
                }

                return products;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Owner owner = new Owner(); 
            Teller teller = new Teller(owner.Products); // Pass the list of products to the Teller
            SalesReportGenerator reportGenerator = new SalesReportGenerator(); 
            SalesReportHandler reportHandler = new SalesReportHandler(reportGenerator); // Pass it to the SalesReportHandler
            do
            {
                LoginPage loginPage = new LoginPage();
                string role = loginPage.DisplayLogin();

                if (role == "owner")
                {
                    while (true)
                    {
                        try
                        {
                            Console.Clear();
                            owner.DisplayProducts();
                            owner.CheckAllStockLevels(); // Call the new method to check stock levels

                            Console.WriteLine("Current Low Threshold: {0}", owner.LowThreshold);
                            Console.WriteLine("Current High Threshold: {0}", owner.HighThreshold);

                            Console.WriteLine("\nOwner Menu: ");
                            Console.WriteLine("[1] Add Product ");
                            Console.WriteLine("[2] Delete Product ");
                            Console.WriteLine("[3] Edit Product Details ");
                            Console.WriteLine("[4] Set Stock Level Checker ");
                            Console.WriteLine("[5] Sales Report ");
                            Console.WriteLine("[6] Login Page ");
                            Console.WriteLine("[7] Exit ");
                            Console.Write("Select an option: ");
                            int choice;
                            while (!int.TryParse(Console.ReadLine(), out choice))
                            {
                                Console.WriteLine("Invalid input. Please enter a number.");
                            }

                            switch (choice)
                            {
                                case 1:
                                    Console.Clear();
                                    owner.DisplayProducts();
                                    Console.Write("Enter Product Name: ");
                                    string name = Console.ReadLine();
                                    Console.Write("Enter Price: ");
                                    decimal price = decimal.Parse(Console.ReadLine());
                                    Console.Write("Enter Stock Level: ");
                                    int stockLevel = int.Parse(Console.ReadLine());
                                    Console.WriteLine("\nExcellent / Very Good / Good / Fair / Poor");
                                    Console.Write("Enter Quality Check: ");
                                    string qualityCheck = Console.ReadLine();
                                    owner.AddProduct(name, price, stockLevel, qualityCheck);
                                    teller = new Teller(owner.Products); 
                                    break;

                                case 2:
                                    Console.Clear();
                                    owner.DisplayProducts();
                                    Console.Write("Enter Product Name to Delete: ");
                                    name = Console.ReadLine();
                                    owner.DeleteProduct(name);
                                    teller = new Teller(owner.Products); // Re-instantiate Teller w/ updated products
                                    break;

                                case 3:
                                    Console.Clear();
                                    owner.DisplayProducts();
                                    Console.Write("Enter Product Name to Edit: ");
                                    name = Console.ReadLine();
                                    Console.Write("Enter New Price: ");
                                    price = decimal.Parse(Console.ReadLine());
                                    Console.Write("Enter New Stock Level: ");
                                    stockLevel = int.Parse(Console.ReadLine());
                                    Console.Write("Enter New Quality Check: ");
                                    qualityCheck = Console.ReadLine();
                                    owner.EditProductDetails(name, price, stockLevel, qualityCheck);
                                    teller = new Teller(owner.Products); // Re-instantiate Teller w/ updated products
                                    break;

                                case 4:
                                    Console.Clear();
                                    Console.WriteLine("Set thresholds for the Stock Level Checker ");
                                    Console.WriteLine("[1] Set Low Threshold ");
                                    Console.WriteLine("[2] Set High Threshold ");
                                    int checkerOpt = int.Parse(Console.ReadLine());

                                    switch (checkerOpt)
                                    {
                                        case 1:
                                            owner.SetLowThreshold(); // Call to set low threshold
                                            break;

                                        case 2:
                                            owner.SetHighThreshold(); // Call to set high threshold
                                            break;
                                        default:
                                            Console.WriteLine("Invalid option!");
                                            Thread.Sleep(1500);
                                            break;
                                    }
                                    break;

                                case 5:
                                    reportHandler.GenerateSalesReport(); // Call the sales report generation method
                                    break;

                                case 6:
                                    break;

                                case 7:
                                    return;

                                default:
                                    Console.WriteLine("Invalid choice. Try again.");
                                    Thread.Sleep(1500);
                                    break;
                            }

                            if (choice == 4)
                            {
                                Console.WriteLine("Please wait...");
                                Thread.Sleep(1500);
                                break;
                            }
                        }
                        catch (IOException e)
                        {
                            Console.WriteLine(e.Message);
                            Thread.Sleep(1500);
                            Console.Clear();
                        }
                        catch (FormatException e)
                        {
                            Console.WriteLine(e.Message);
                            Thread.Sleep(1500);
                            Console.Clear();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Thread.Sleep(1500);
                        }
                    }
                }
                else if (role == "teller")
                {
                    while (true)
                    {
                        try
                        {
                            Console.Clear();
                            owner.DisplayProducts();
                            Console.WriteLine("Teller Menu: ");
                            Console.WriteLine("[1] Place Order ");
                            Console.WriteLine("[2] Search Product ");
                            Console.WriteLine("[3] Login Page ");
                            Console.WriteLine("[4] Exit ");
                            owner.CheckAllStockLevels(); // Call the new method to check stock levels
                            Console.Write("Select an option: ");
                            int choice = int.Parse(Console.ReadLine());

                            switch (choice)
                            {
                                case 1:
                                    Console.Clear();
                                    owner.DisplayProducts();
                                    teller.Order(); //update stock level in the same products list
                                    break;

                                case 2:
                                    Console.Clear();
                                    owner.DisplayProducts();
                                    Console.Write("Enter Product Name to Search: ");
                                    string productName = Console.ReadLine();
                                    teller.SearchProduct(productName);
                                    break;

                                case 3:
                                    break;

                                case 4:
                                    return;

                                default:
                                    Console.WriteLine("Invalid choice. Try again.");
                                    Thread.Sleep(1500);
                                    break;
                            }

                            if (choice == 3)
                            {
                                Console.WriteLine("Please wait...");
                                Thread.Sleep(1500);
                                break;
                            }
                        }
                        catch (IOException e)
                        {
                            Console.WriteLine(e.Message);
                            Thread.Sleep(1500);
                            Console.Clear();
                        }
                        catch (FormatException e)
                        {
                            Console.WriteLine(e.Message);
                            Thread.Sleep(1500);
                            Console.Clear();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Thread.Sleep(1500);
                            Console.Clear();
                        }
                    }
                }
                else if (role == "secret")
                {
                    loginPage.ChangePassword();
                }
            } while (true);
        }
    }
}