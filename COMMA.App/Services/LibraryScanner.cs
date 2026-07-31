using System.Collections.Generic;
using System.IO;
using COMMA.App.Models;

namespace COMMA.App.Services;

public class LibraryScanner
{
    public List<Product> Scan(string folder)
    {
        var products = new List<Product>();

        if (!Directory.Exists(folder))
            return products;

        foreach (var dir in Directory.GetDirectories(folder))
        {
            products.Add(new Product
            {
                Code = Path.GetFileName(dir),
                Folder = dir
            });
        }

        products.Sort((a, b) => string.Compare(a.Code, b.Code));

        return products;
    }
}