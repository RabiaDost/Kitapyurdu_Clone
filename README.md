Kitapyurdu Clone Project
------------------------

1. About the Project:
   This project is a clone of Kitapyurdu.com website with its basic features.
   Users can register to the site, search and view books, list books by categories, add to cart,
   place orders, view these orders, and make comments.

2. Technologies Used:
   - ASP.NET Core Web (Razor Pages)
   - Microsoft SQL Server
   - HTML, CSS, JavaScript

3. Installation Steps:
   a) Database Setup:
      - Open SQL Server Management Studio
      - Restore the DbKitapYurdu.bak file from the Database folder

   b) Project Setup:
      - Open Visual Studio 2022
      - Open the project
      - Modify the connection string in appsettings.json according to your SQL Server:
        "DefaultConnection": "Server=DESKTOP-BT8G78S\\SQLEXPRESS;Database=DbKitapYurdu;Trusted_Connection=True;MultipleActiveResultSets=true"
      - Run the project

4. Features:
   - Listing Books, Magazines, Hobby & Toys, Stationery products
   - Retrieving books by category in all categories
   - Book detail viewing
   - Magazine detail viewing
   - Commenting and rating
   - Adding to cart
   - Creating orders and viewing created orders

5. Database Structure:
   - Users: User information
   - Books: Book information
   - Magazines: Magazine information
   - Categories: Category types
   - Reviews: User reviews
   - Orders: Order information
   - OrderDetails: Order details

6. Stored Procedures:
   - AddOrder: Creates an order with user ID, product ID, and product quantity
   - AddUser: Adds a user
   - GetBooksByCategory: Retrieves books by their categories
   - GetUserOrders: Retrieves orders of the user with given ID
   - UpdateBookStock: Updates stock quantity for the product with given ID

7. Triggers:
   - trg_UpdateBookRating: Updates rating when a comment is added
   - trg_UserAdded: Adds users
   - trg_LowStockAlert: Monitors low stock situations

8. Views:
   - vw_BookReviewStats: Shows book review statistics

9. Functions:
   - IsBookInStock: Checks stock availability
   - GetFullName: Concatenates user's first and last name
   - CalculateTotalPrice: Calculates total price
