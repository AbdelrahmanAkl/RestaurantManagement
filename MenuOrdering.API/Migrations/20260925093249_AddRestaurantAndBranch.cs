using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuOrdering.API.Migrations
{
    public partial class AddRestaurantAndBranch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_TableNumber",
                table: "RestaurantTables");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CategoryId",
                table: "MenuItems");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "RestaurantTables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RestaurantId",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Create default restaurant first.
            migrationBuilder.CreateTable(
                name: "Restaurants",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation(
                            "SqlServer:Identity",
                            "1, 1"),

                    Name = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: false),

                    Description = table.Column<string>(
                        type: "nvarchar(1000)",
                        maxLength: 1000,
                        nullable: true),

                    Phone = table.Column<string>(
                        type: "nvarchar(50)",
                        maxLength: 50,
                        nullable: true),

                    Email = table.Column<string>(
                        type: "nvarchar(200)",
                        maxLength: 200,
                        nullable: true),

                    IsActive = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    CreatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Restaurants",
                        x => x.Id);
                });

            // Create default branch.
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation(
                            "SqlServer:Identity",
                            "1, 1"),

                    RestaurantId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Name = table.Column<string>(
                        type: "nvarchar(150)",
                        maxLength: 150,
                        nullable: false),

                    Address = table.Column<string>(
                        type: "nvarchar(500)",
                        maxLength: 500,
                        nullable: true),

                    Phone = table.Column<string>(
                        type: "nvarchar(50)",
                        maxLength: 50,
                        nullable: true),

                    IsActive = table.Column<bool>(
                        type: "bit",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Branches",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_Branches_Restaurants_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create a default restaurant.
            migrationBuilder.Sql("""
                SET IDENTITY_INSERT Restaurants ON;

                INSERT INTO Restaurants
                (
                    Id,
                    Name,
                    Description,
                    Phone,
                    Email,
                    IsActive,
                    CreatedAt
                )
                VALUES
                (
                    1,
                    'Default Restaurant',
                    'Default restaurant created during SaaS migration.',
                    NULL,
                    NULL,
                    1,
                    GETUTCDATE()
                );

                SET IDENTITY_INSERT Restaurants OFF;
                """);

            // Create a default branch.
            migrationBuilder.Sql("""
                SET IDENTITY_INSERT Branches ON;

                INSERT INTO Branches
                (
                    Id,
                    RestaurantId,
                    Name,
                    Address,
                    Phone,
                    IsActive
                )
                VALUES
                (
                    1,
                    1,
                    'Main Branch',
                    NULL,
                    NULL,
                    1
                );

                SET IDENTITY_INSERT Branches OFF;
                """);

            // Move existing data to the default tenant.
            migrationBuilder.Sql("""
                UPDATE Categories
                SET RestaurantId = 1
                WHERE RestaurantId = 0;
                """);

            migrationBuilder.Sql("""
                UPDATE RestaurantTables
                SET BranchId = 1
                WHERE BranchId = 0;
                """);

            migrationBuilder.Sql("""
                UPDATE Orders
                SET BranchId = 1
                WHERE BranchId = 0;
                """);

            // Existing users become users of the default restaurant/branch.
            // Only users that don't already have tenant information are updated.
            migrationBuilder.Sql("""
                UPDATE Users
                SET
                    RestaurantId = 1,
                    BranchId = 1
                WHERE RestaurantId IS NULL;
                """);

            // Indexes
            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId",
                table: "Users",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RestaurantId",
                table: "Users",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_BranchId_TableNumber",
                table: "RestaurantTables",
                columns: new[]
                {
                    "BranchId",
                    "TableNumber"
                },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId",
                table: "Orders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId_Name",
                table: "MenuItems",
                columns: new[]
                {
                    "CategoryId",
                    "Name"
                },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_RestaurantId_Name",
                table: "Categories",
                columns: new[]
                {
                    "RestaurantId",
                    "Name"
                },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_RestaurantId_Name",
                table: "Branches",
                columns: new[]
                {
                    "RestaurantId",
                    "Name"
                },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_Name",
                table: "Restaurants",
                column: "Name",
                unique: true);

            // Foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Restaurants_RestaurantId",
                table: "Categories",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Branches_BranchId",
                table: "Orders",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantTables_Branches_BranchId",
                table: "RestaurantTables",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Branches_BranchId",
                table: "Users",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Restaurants_RestaurantId",
                table: "Users",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Restaurants_RestaurantId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Branches_BranchId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantTables_Branches_BranchId",
                table: "RestaurantTables");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Branches_BranchId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Restaurants_RestaurantId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Restaurants");

            migrationBuilder.DropIndex(
                name: "IX_Users_BranchId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RestaurantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_BranchId_TableNumber",
                table: "RestaurantTables");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BranchId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CategoryId_Name",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_Categories_RestaurantId_Name",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "Categories");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_TableNumber",
                table: "RestaurantTables",
                column: "TableNumber",
                unique: true);

           migrationBuilder.CreateIndex(
    name: "IX_MenuItems_CategoryId",
    table: "MenuItems",
    column: "CategoryId");
        }
    }
}