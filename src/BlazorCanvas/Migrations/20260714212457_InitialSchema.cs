using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BlazorCanvas.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    username = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "figures",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    x1 = table.Column<int>(type: "integer", nullable: false),
                    y1 = table.Column<int>(type: "integer", nullable: false),
                    x2 = table.Column<int>(type: "integer", nullable: false),
                    y2 = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_figures", x => x.id);
                    table.CheckConstraint("box_is_a_box", "type NOT IN ('rectangle','triangle') OR (x2 > x1 AND y2 > y1)");
                    table.CheckConstraint("circle_is_a_circle", "type <> 'circle' OR (x2 - x1 = y2 - y1 AND x2 > x1 AND (x2 - x1) % 2 = 0)");
                    table.CheckConstraint("figures_type_is_known", "type IN ('line','rectangle','circle','triangle')");
                    table.CheckConstraint("line_is_a_line", "type <> 'line' OR (x2 >= x1 AND (x2 > x1 OR y2 <> y1))");
                    table.ForeignKey(
                        name: "FK_figures_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "x1,y1,x2,y2 are ALWAYS the figure's bounding box. A CIRCLE is stored as the square it is inscribed in: r = (x2-x1)/2, cx = x1+r, cy = y1+r. It is DRAWN centre-out (press centre, drag for radius) but STORED as a square — interaction and storage are different things. A LINE is the segment between the two points and may run diagonally in either vertical direction; it is normalised by swapping the whole point pair, never by sorting axes.");

            migrationBuilder.CreateIndex(
                name: "ix_figures_user_id",
                table: "figures",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "figures");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
