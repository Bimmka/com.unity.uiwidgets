using Unity.UIWidgets.foundation;

namespace UIWidgetsGallery.demo.shrine.model
{
    public enum Category
    {
        all,
        accessories,
        clothing,
        home,
    }

    public class Product
    {
        public Product(
            Category category,
            int id,
            bool isFeatured,
            string name,
            int price
        )
        {
            D.assert(name != null);
            this.category = category;
            this.id = id;
            this.isFeatured = isFeatured;
            this.name = name;
            this.price = price;
        }

        public readonly Category category;
        public readonly int id;
        public readonly bool isFeatured;
        public readonly string name;
        public readonly int price;

        private string assetName => $"{this.id}-0.jpg";
        private string assetPackage => "shrine_images";


        public override string ToString()
        {
            return $"{this.name} (id={this.id})";
        }
    }
}