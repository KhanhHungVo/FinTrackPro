using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrackPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitFamilyChildrenCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""TransactionCategories""
                SET ""Slug"" = 'family', ""LabelEn"" = 'Family', ""LabelVi"" = 'Gia đình'
                WHERE ""Slug"" = 'family_child' AND ""IsSystem"" = TRUE;

                UPDATE ""TransactionCategories""
                SET ""SortOrder"" = 12
                WHERE ""Slug"" = 'other_expense' AND ""IsSystem"" = TRUE;

                INSERT INTO ""TransactionCategories"" (""Id"", ""UserId"", ""Type"", ""Slug"", ""LabelEn"", ""LabelVi"", ""Icon"", ""IsSystem"", ""IsActive"", ""SortOrder"", ""CreatedAt"")
                VALUES (gen_random_uuid(), NULL, 1, 'children', 'Children', 'Trẻ em', '🧒', TRUE, TRUE, 11, NOW() AT TIME ZONE 'UTC');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""TransactionCategories"" WHERE ""Slug"" = 'children' AND ""IsSystem"" = TRUE;

                UPDATE ""TransactionCategories""
                SET ""SortOrder"" = 11
                WHERE ""Slug"" = 'other_expense' AND ""IsSystem"" = TRUE;

                UPDATE ""TransactionCategories""
                SET ""Slug"" = 'family_child', ""LabelEn"" = 'Family & Children', ""LabelVi"" = 'Gia đình & trẻ em'
                WHERE ""Slug"" = 'family' AND ""IsSystem"" = TRUE;
            ");
        }
    }
}
