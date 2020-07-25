using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProductShop.Data;
using ProductShop.Dtos.Export;
using ProductShop.Dtos.Import;
using ProductShop.Models;
using ProductShop.XMLHelper;

namespace ProductShop
{
    public class StartUp
    {
        public const string ResultDirectoryPath = "../../../Datasets/Results";

        public static void Main(string[] args)
        {
            ProductShopContext context = new ProductShopContext();

            //just in the begining to create DB:
            //ResetDataBase(context);

            //queries 1÷4:
            var usersXml = File.ReadAllText("../../../Datasets/users.xml");
            var productsXml = File.ReadAllText("../../../Datasets/products.xml");
            var categoriesXml = File.ReadAllText("../../../Datasets/categories.xml");
            var categoriesProductsXml = File.ReadAllText("../../../Datasets/categories-products.xml");

            //queries 5÷8:
            var productsInRange = GetSoldProducts(context);

            if (!Directory.Exists(ResultDirectoryPath))
            {
                Directory.CreateDirectory(ResultDirectoryPath);
            }

            File.WriteAllText("../../../Datasets/Results/users-sold-products.xml", productsInRange);


        }

        public static void ResetDataBase(ProductShopContext db)
        {
            db.Database.EnsureDeleted();
            Console.WriteLine("DB deleted");
            db.Database.EnsureCreated();
            Console.WriteLine("DB created");

        }

        //Query 1. Import Users
        public static string ImportUsers(ProductShopContext context, string inputXml)
        {

            //var result = ImportUsers(context, usersXml);
            //Console.WriteLine(result);

            const string rootElement = "Users";

            var usersResult = XMLConverter.Deserializer<ImportUserDto>(inputXml, rootElement);

            //this below is first version of input users by foreach

            //var users = new List<User>();

            //foreach (var importUserDto in users)
            //{
            //    var user = new User
            //    {
            //        FirstName = importUserDto.FirstName,
            //        LastName = importUserDto.LastName,
            //        Age = importUserDto.Age

            //    };
            //    users.Add(user);
            //}

            var users = usersResult
                .Select(u => new User
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Age = u.Age
                })
                .ToArray();


            context.Users.AddRange(users);
            int usersCount = context.SaveChanges();

            return $"Successfully imported {usersCount}";
        }

        //Query 2. Import Products
        public static string ImportProducts(ProductShopContext context, string inputXml)
        {

            //var result = ImportProducts(context, productsXml);
            //Console.WriteLine(result);

            const string rootElement = "Products";

            var productsResult = XMLConverter.Deserializer<ImportProductDto>(inputXml, rootElement);

            var products = productsResult
                .Select(p => new Product
                {
                    Name = p.Name,
                    Price = p.Price,
                    SellerId = p.SellerId,
                    BuyerId = p.BuyerId
                })
                .ToArray();

            context.Products.AddRange(products);
            int productsCount = context.SaveChanges();

            return $"Successfully imported {productsCount}";
        }

        //Query 3. Import Categories
        public static string ImportCategories(ProductShopContext context, string inputXml)
        {

            //var result = ImportCategories(context, categoriesXml);
            //Console.WriteLine(result);

            const string rootElement = "Categories";

            var categoryResult = XMLConverter.Deserializer<CategoryDto>(inputXml, rootElement);

            //var categories = new List<Category>();

            //foreach (var cat in categoryResult)
            //{
            //    if (cat.Name == null)
            //    {
            //        continue;
            //    }

            //    var category = new Category
            //    {
            //        Name = cat.Name,
            //    };

            //    categories.Add(category);
            //}

            var categories = categoryResult
                .Where(c => c.Name != null)
                .Select(c => new Category
                {
                    Name = c.Name
                })
                .ToArray();

            context.Categories.AddRange(categories);
            int categoriesCount = context.SaveChanges();

            return $"Successfully imported {categoriesCount}";
        }

        //Query 4. Import Categories and Products
        public static string ImportCategoryProducts(ProductShopContext context, string inputXml)
        {

            //var result = ImportCategoryProducts(context, categoriesProductsXml);
            //Console.WriteLine(result);

            const string rootElement = "CategoryProducts";

            var catProdResult = XMLConverter.Deserializer<CategoriesProductsDto>(inputXml, rootElement);

            var categoriesProducts = catProdResult
                .Where(c => context.Categories.Any(s => s.Id == c.CategoryId) && context.Products.Any(s => s.Id == c.ProductId))
                .Select(cp => new CategoryProduct
                {
                    CategoryId = cp.CategoryId,
                    ProductId = cp.ProductId,
                })
                .ToArray();


            //other possible version
            //var categoriesProducts = new List<CategoryProduct>();

            //foreach (var catProd in catProdResult)
            //{
            //    var doesExist = context.Products.Any(x => x.Id == catProd.ProductId) && context.Categories.Any(x => x.Id == catProd.CategoryId);

            //    if (!doesExist)
            //    {
            //        continue;
            //    }

            //    var categoryProduct = new CategoryProduct
            //    {
            //        CategoryId = catProd.CategoryId,
            //        ProductId = catProd.ProductId
            //    };

            //    categoriesProducts.Add(categoryProduct);
            //}

            context.CategoryProducts.AddRange(categoriesProducts);
            int catProdCount = context.SaveChanges();


            return $"Successfully imported {catProdCount}";
        }

        //Query 5. Products In Range

        public static string GetProductsInRange(ProductShopContext context)
        {
            //var productsInRange = GetProductsInRange(context);

            //if (!Directory.Exists(ResultDirectoryPath))
            //{
            //    Directory.CreateDirectory(ResultDirectoryPath);
            //}

            //File.WriteAllText("../../../Datasets/Results/products-in-range.xml", productsInRange);


            const string rootElement = "Products";

            var products = context.Products
              .Where(p => p.Price >= 500 && p.Price <= 1000)
              .Select(p => new ExportProductDto
              {
                  Name = p.Name,
                  Price = p.Price,
                  Buyer = p.Buyer.FirstName + " " + p.Buyer.LastName
              })
              .OrderBy(p => p.Price)
              .Take(10)
              .ToArray();

            var result = XMLConverter.Serialize(products, rootElement);

            return result;
        }

        //Query 6. Sold Products
        public static string GetSoldProducts(ProductShopContext context)
        {
            //var productsInRange = GetSoldProducts(context);

            //if (!Directory.Exists(ResultDirectoryPath))
            //{
            //    Directory.CreateDirectory(ResultDirectoryPath);
            //}

            //File.WriteAllText("../../../Datasets/Results/users-sold-products.xml", productsInRange);

            const string rootElement = "Users";

            var usersWithProducts = context.Users
                .Where(u => u.ProductsSold.Any())
                .Select(u => new ExportUserSoldProductDto
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    SoldProducts = u.ProductsSold.Select(sp => new UserProductDto
                    {
                        Name = sp.Name,
                        Price = sp.Price
                    })
                    .ToArray()
                })
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Take(5)
                .ToArray();


            var result = XMLConverter.Serialize(usersWithProducts, rootElement);

            return result;

        }

        //Query 7. Categories By Products Count
        public static string GetCategoriesByProductsCount(ProductShopContext context)
        {
            //var productsInRange = GetCategoriesByProductsCount(context);

            //if (!Directory.Exists(ResultDirectoryPath))
            //{
            //    Directory.CreateDirectory(ResultDirectoryPath);
            //}

            //File.WriteAllText("../../../Datasets/Results/categories-by-products.xml", productsInRange);

            const string rootElement = "Categories";

            var exportUserSoldProductDtos = context.Categories
                .Select(p => new ExportCategoriesByProductCountDto
                {
                    Name = p.Name,
                    Count = p.CategoryProducts.Count(),
                    AveragePrice = p.CategoryProducts.Average(pr => pr.Product.Price),
                    TotalRevenue = p.CategoryProducts.Sum(pr => pr.Product.Price)
                })
                .OrderByDescending(p => p.Count)
                .ThenBy(p => p.TotalRevenue)
                .ToArray();


            var result = XMLConverter.Serialize(exportUserSoldProductDtos, rootElement);

            return result;
        }

        //Query 8. Users and Products
        public static string GetUsersWithProducts(ProductShopContext context)
        {

            //var productsInRange = GetUsersWithProducts(context);

            //if (!Directory.Exists(ResultDirectoryPath))
            //{
            //    Directory.CreateDirectory(ResultDirectoryPath);
            //}

            //File.WriteAllText("../../../Datasets/Results/users-and-products.xml", productsInRange);

            const string rootElement = "Users";

            var uasersAndProducts = context.Users
                .ToArray()
                .Where(u => u.ProductsSold.Any())
                .Select(u => new ExportUserDto
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Age = u.Age,
                    SoldProduct = new ExportProductCountDto
                    {
                        Count = u.ProductsSold.Count(),
                        Products = u.ProductsSold.Select(cp => new ExportCountedProductDto
                        {
                            Name = cp.Name,
                            Price = cp.Price
                        })
                        .OrderByDescending(p => p.Price)
                        .ToArray()
                    }

                })
                .OrderByDescending(p => p.SoldProduct.Count)
                .Take(10)
                .ToArray();

            var resultCount = new ExporUsersCount
            {
                Count = context.Users.Count(u => u.ProductsSold.Any()),
                Users = uasersAndProducts
            };


            var result = XMLConverter.Serialize(resultCount, rootElement);

            return result;
        }
    }
}