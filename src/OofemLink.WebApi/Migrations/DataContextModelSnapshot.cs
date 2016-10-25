using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using OofemLink.Data;

namespace OofemLink.WebApi.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.1")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("OofemLink.Data.MeshEntities.Beam", b =>
                {
                    b.Property<int>("Id");

                    b.Property<int>("MeshId");

                    b.Property<int>("EndNodeId");

                    b.Property<int>("StartNodeId");

                    b.HasKey("Id", "MeshId");

                    b.HasIndex("MeshId");

                    b.ToTable("Beams");
                });

            modelBuilder.Entity("OofemLink.Data.MeshEntities.Mesh", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("ModelId");

                    b.HasKey("Id");

                    b.HasIndex("ModelId");

                    b.ToTable("Meshes");
                });

            modelBuilder.Entity("OofemLink.Data.MeshEntities.Node", b =>
                {
                    b.Property<int>("Id");

                    b.Property<int>("MeshId");

                    b.Property<double>("X");

                    b.Property<double>("Y");

                    b.Property<double>("Z");

                    b.HasKey("Id", "MeshId");

                    b.HasIndex("MeshId");

                    b.ToTable("Nodes");
                });

            modelBuilder.Entity("OofemLink.Data.ModelEntities.Curve", b =>
                {
                    b.Property<int>("Id");

                    b.Property<int>("ModelId");

                    b.Property<int>("MacroId");

                    b.HasKey("Id", "ModelId");

                    b.HasIndex("ModelId");

                    b.HasIndex("MacroId", "ModelId");

                    b.ToTable("Curves");
                });

            modelBuilder.Entity("OofemLink.Data.ModelEntities.Macro", b =>
                {
                    b.Property<int>("Id");

                    b.Property<int>("ModelId");

                    b.HasKey("Id", "ModelId");

                    b.HasIndex("ModelId");

                    b.ToTable("Macros");
                });

            modelBuilder.Entity("OofemLink.Data.ModelEntities.Model", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.HasKey("Id");

                    b.ToTable("Models");
                });

            modelBuilder.Entity("OofemLink.Data.ModelEntities.Surface", b =>
                {
                    b.Property<int>("Id");

                    b.Property<int>("ModelId");

                    b.Property<int>("MacroId");

                    b.HasKey("Id", "ModelId");

                    b.HasIndex("ModelId");

                    b.HasIndex("MacroId", "ModelId");

                    b.ToTable("Surfaces");
                });

            modelBuilder.Entity("OofemLink.Data.ModelEntities.Vertex", b =>
                {
                    b.Property<int>("Id");

                    b.Property<int>("ModelId");

                    b.Property<int>("MacroId");

                    b.Property<double>("X");

                    b.Property<double>("Y");

                    b.Property<double>("Z");

                    b.HasKey("Id", "ModelId");

                    b.HasIndex("ModelId");

                    b.HasIndex("MacroId", "ModelId");

                    b.ToTable("Vertices");
                });

            modelBuilder.Entity("OofemLink.Data.Project", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("OofemLink.Data.Simulation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("ProjectId");

                    b.Property<int>("State");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("Simulations");
                });

            modelBuilder.Entity("OofemLink.Data.MeshEntities.Beam", b =>
                {
                    b.HasOne("OofemLink.Data.MeshEntities.Mesh", "Mesh")
                        .WithMany("Beams")
                        .HasForeignKey("MeshId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("OofemLink.Data.MeshEntities.Mesh", b =>
                {
                    b.HasOne("OofemLink.Data.ModelEntities.Model", "Model")
                        .WithMany()
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("OofemLink.Data.MeshEntities.Node", b =>
                {
                    b.HasOne("OofemLink.Data.MeshEntities.Mesh", "Mesh")
                        .WithMany("Nodes")
                        .HasForeignKey("MeshId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("OofemLink.Data.ModelEntities.Curve", b =>
                {
                    b.HasOne("OofemLink.Data.ModelEntities.Model", "Model")
                        .WithMany()
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("OofemLink.Data.ModelEntities.Macro", "Macro")
                        .WithMany("Curves")
                        .HasForeignKey("MacroId", "ModelId");
                });

            modelBuilder.Entity("OofemLink.Data.ModelEntities.Macro", b =>
                {
                    b.HasOne("OofemLink.Data.ModelEntities.Model", "Model")
                        .WithMany("Macros")
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("OofemLink.Data.ModelEntities.Surface", b =>
                {
                    b.HasOne("OofemLink.Data.ModelEntities.Model", "Model")
                        .WithMany()
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("OofemLink.Data.ModelEntities.Macro", "Macro")
                        .WithMany("Surfaces")
                        .HasForeignKey("MacroId", "ModelId");
                });

            modelBuilder.Entity("OofemLink.Data.ModelEntities.Vertex", b =>
                {
                    b.HasOne("OofemLink.Data.ModelEntities.Model", "Model")
                        .WithMany()
                        .HasForeignKey("ModelId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("OofemLink.Data.ModelEntities.Macro", "Macro")
                        .WithMany("Vertices")
                        .HasForeignKey("MacroId", "ModelId");
                });

            modelBuilder.Entity("OofemLink.Data.Simulation", b =>
                {
                    b.HasOne("OofemLink.Data.Project", "Project")
                        .WithMany("Simulations")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
