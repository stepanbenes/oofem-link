using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OofemLink.WebApi.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Models",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Models", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meshes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ModelId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meshes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meshes_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Macros",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    ModelId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Macros", x => new { x.Id, x.ModelId });
                    table.ForeignKey(
                        name: "FK_Macros_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Simulations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProjectId = table.Column<int>(nullable: false),
                    State = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Simulations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Simulations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Beams",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    MeshId = table.Column<int>(nullable: false),
                    Node1Id = table.Column<int>(nullable: false),
                    Node2Id = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beams", x => new { x.Id, x.MeshId });
                    table.ForeignKey(
                        name: "FK_Beams_Meshes_MeshId",
                        column: x => x.MeshId,
                        principalTable: "Meshes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    MeshId = table.Column<int>(nullable: false),
                    X = table.Column<double>(nullable: false),
                    Y = table.Column<double>(nullable: false),
                    Z = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => new { x.Id, x.MeshId });
                    table.ForeignKey(
                        name: "FK_Nodes_Meshes_MeshId",
                        column: x => x.MeshId,
                        principalTable: "Meshes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Curves",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    ModelId = table.Column<int>(nullable: false),
                    MacroId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Curves", x => new { x.Id, x.ModelId });
                    table.ForeignKey(
                        name: "FK_Curves_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Curves_Macros_MacroId_ModelId",
                        columns: x => new { x.MacroId, x.ModelId },
                        principalTable: "Macros",
                        principalColumns: new[] { "Id", "ModelId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Surfaces",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    ModelId = table.Column<int>(nullable: false),
                    MacroId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surfaces", x => new { x.Id, x.ModelId });
                    table.ForeignKey(
                        name: "FK_Surfaces_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Surfaces_Macros_MacroId_ModelId",
                        columns: x => new { x.MacroId, x.ModelId },
                        principalTable: "Macros",
                        principalColumns: new[] { "Id", "ModelId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vertices",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    ModelId = table.Column<int>(nullable: false),
                    MacroId = table.Column<int>(nullable: false),
                    X = table.Column<double>(nullable: false),
                    Y = table.Column<double>(nullable: false),
                    Z = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vertices", x => new { x.Id, x.ModelId });
                    table.ForeignKey(
                        name: "FK_Vertices_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vertices_Macros_MacroId_ModelId",
                        columns: x => new { x.MacroId, x.ModelId },
                        principalTable: "Macros",
                        principalColumns: new[] { "Id", "ModelId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beams_MeshId",
                table: "Beams",
                column: "MeshId");

            migrationBuilder.CreateIndex(
                name: "IX_Meshes_ModelId",
                table: "Meshes",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_MeshId",
                table: "Nodes",
                column: "MeshId");

            migrationBuilder.CreateIndex(
                name: "IX_Curves_ModelId",
                table: "Curves",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Curves_MacroId_ModelId",
                table: "Curves",
                columns: new[] { "MacroId", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Macros_ModelId",
                table: "Macros",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Surfaces_ModelId",
                table: "Surfaces",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Surfaces_MacroId_ModelId",
                table: "Surfaces",
                columns: new[] { "MacroId", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Vertices_ModelId",
                table: "Vertices",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Vertices_MacroId_ModelId",
                table: "Vertices",
                columns: new[] { "MacroId", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Simulations_ProjectId",
                table: "Simulations",
                column: "ProjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Beams");

            migrationBuilder.DropTable(
                name: "Nodes");

            migrationBuilder.DropTable(
                name: "Curves");

            migrationBuilder.DropTable(
                name: "Surfaces");

            migrationBuilder.DropTable(
                name: "Vertices");

            migrationBuilder.DropTable(
                name: "Simulations");

            migrationBuilder.DropTable(
                name: "Meshes");

            migrationBuilder.DropTable(
                name: "Macros");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Models");
        }
    }
}
