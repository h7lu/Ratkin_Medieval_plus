using Verse;

namespace RkM;
public abstract class ModExtension_NutritionRecipe : DefModExtension
{
    public int nutrition = -1;
    public virtual int Nutrition => nutrition;
}
public class ModExtension_NutritionStorageRecipe : ModExtension_NutritionRecipe
{
}
public class ModExtension_NutritionConsumeRecipe : ModExtension_NutritionRecipe
{
}
public class ModExtension_WithoutNutritionGetRecipe : ModExtension_NutritionRecipe
{
    public bool useIngredients = false;
    public bool matchDispensable = true;
}