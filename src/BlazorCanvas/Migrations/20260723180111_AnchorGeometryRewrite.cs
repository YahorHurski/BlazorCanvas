using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorCanvas.Migrations
{
    /// <inheritdoc />
    public partial class AnchorGeometryRewrite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_figures_user_id",
                table: "figures");

            migrationBuilder.AddColumn<Guid>(
                name: "id_new",
                table: "figures",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<int>(
                name: "x",
                table: "figures",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "y",
                table: "figures",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "geometry",
                table: "figures",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "z",
                table: "figures",
                type: "numeric",
                nullable: true);

            migrationBuilder.Sql(
                """
                -- Backfill mirrors GeometryCodec exactly:
                -- rectangle/triangle: anchor x1,y1 + {w,h}; circle: centre anchor + {r}; line: first endpoint + signed {dx,dy}; z = old integer id.
                UPDATE figures
                SET x = x1,
                    y = y1,
                    geometry = jsonb_build_object('w', x2 - x1, 'h', y2 - y1),
                    z = id::numeric
                WHERE type IN ('rectangle', 'triangle');

                UPDATE figures
                SET x = x1 + ((x2 - x1) / 2),
                    y = y1 + ((y2 - y1) / 2),
                    geometry = jsonb_build_object('r', (x2 - x1) / 2),
                    z = id::numeric
                WHERE type = 'circle';

                UPDATE figures
                SET x = x1,
                    y = y1,
                    geometry = jsonb_build_object('dx', x2 - x1, 'dy', y2 - y1),
                    z = id::numeric
                WHERE type = 'line';
                """);

            migrationBuilder.AlterColumn<int>(
                name: "x",
                table: "figures",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "y",
                table: "figures",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "geometry",
                table: "figures",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "z",
                table: "figures",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.DropCheckConstraint(
                name: "box_is_a_box",
                table: "figures");

            migrationBuilder.DropCheckConstraint(
                name: "circle_is_a_circle",
                table: "figures");

            migrationBuilder.DropCheckConstraint(
                name: "line_is_a_line",
                table: "figures");

            migrationBuilder.DropColumn(
                name: "x1",
                table: "figures");

            migrationBuilder.DropColumn(
                name: "y1",
                table: "figures");

            migrationBuilder.DropColumn(
                name: "x2",
                table: "figures");

            migrationBuilder.DropColumn(
                name: "y2",
                table: "figures");

            migrationBuilder.DropPrimaryKey(
                name: "PK_figures",
                table: "figures");

            migrationBuilder.DropColumn(
                name: "id",
                table: "figures");

            migrationBuilder.RenameColumn(
                name: "id_new",
                table: "figures",
                newName: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_figures",
                table: "figures",
                column: "id");

            migrationBuilder.AlterTable(
                name: "figures",
                comment: "A figure is stored as anchor x,y plus geometry jsonb relative to that anchor: circle {r} about the centre, rectangle/triangle {w,h}, line {dx,dy}. Geometry has no database CHECK; the server is the sole writer and constructs well-formed JSON in code.",
                oldComment: "x1,y1,x2,y2 are ALWAYS the figure's bounding box. A CIRCLE is stored as the square it is inscribed in: r = (x2-x1)/2, cx = x1+r, cy = y1+r. It is DRAWN centre-out (press centre, drag for radius) but STORED as a square — interaction and storage are different things. A LINE is the segment between the two points and may run diagonally in either vertical direction; it is normalised by swapping the whole point pair, never by sorting axes.");

            migrationBuilder.CreateIndex(
                name: "ix_figures_user_id_z",
                table: "figures",
                columns: new[] { "user_id", "z" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    RAISE EXCEPTION 'AnchorGeometryRewrite is an irreversible data migration; restore from backup or fixture instead of rolling it down.';
                END $$;
                """);
        }
    }
}
