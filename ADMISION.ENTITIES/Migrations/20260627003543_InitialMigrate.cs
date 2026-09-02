using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "DocumentaryManagement");

            migrationBuilder.EnsureSchema(
                name: "System");

            migrationBuilder.EnsureSchema(
                name: "Integrations");

            migrationBuilder.EnsureSchema(
                name: "Info");

            migrationBuilder.EnsureSchema(
                name: "Modality");

            migrationBuilder.EnsureSchema(
                name: "Infrastructure");

            migrationBuilder.EnsureSchema(
                name: "Ubigeo");

            migrationBuilder.EnsureSchema(
                name: "Exam");

            migrationBuilder.EnsureSchema(
                name: "Requirement");

            migrationBuilder.EnsureSchema(
                name: "Postulant");

            migrationBuilder.EnsureSchema(
                name: "Biometrics");

            migrationBuilder.EnsureSchema(
                name: "EconomicManagement");

            migrationBuilder.EnsureSchema(
                name: "Notifications");

            migrationBuilder.EnsureSchema(
                name: "Users");

            migrationBuilder.EnsureSchema(
                name: "Schools");

            migrationBuilder.CreateTable(
                name: "AcademicYearName",
                schema: "DocumentaryManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYearName", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccessLog",
                schema: "System",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    RequestPath = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ResponseCode = table.Column<int>(type: "integer", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Audit",
                schema: "System",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TableName = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Banner",
                schema: "Info",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    ImageUrlVertical = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banner", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Config",
                schema: "System",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Config", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Country",
                schema: "Ubigeo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DisabilityTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisabilityTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentHeaderConfig",
                schema: "DocumentaryManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstitutionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Dependency = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OfficeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Ruc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    SecondaryLogoUrl = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    FooterText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentHeaderConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentType",
                schema: "DocumentaryManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TemplateName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CorrelativePrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CorrelativePadding = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalApi",
                schema: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    HttpMethod = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    AuthType = table.Column<string>(type: "text", nullable: false),
                    AuthHeaderName = table.Column<string>(type: "text", nullable: true),
                    AuthValue = table.Column<string>(type: "text", nullable: true),
                    RequestParametersJson = table.Column<string>(type: "text", nullable: true),
                    HeadersJson = table.Column<string>(type: "text", nullable: true),
                    RequestBodyTemplate = table.Column<string>(type: "text", nullable: true),
                    ResponseFieldsJson = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalApi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Faculty",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculty", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FaqItem",
                schema: "Info",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Keywords = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    HitCount = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaqItem_FaqItem_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "Info",
                        principalTable: "FaqItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileRequirementManagement",
                schema: "Requirement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    FilePathExtencion = table.Column<string>(type: "text", nullable: false),
                    MaxFileSizeMB = table.Column<decimal>(type: "numeric", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileRequirementManagement", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MethodPayments",
                schema: "EconomicManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodPayments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    ActionUrl = table.Column<string>(type: "text", nullable: true),
                    EntityType = table.Column<string>(type: "text", nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IconClass = table.Column<string>(type: "text", nullable: true),
                    ColorScheme = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OtherFiles",
                schema: "Info",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pavilion",
                schema: "Infrastructure",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pavilion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rols",
                schema: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TematicArea",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TematicArea", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Terms",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypePostulantInscription",
                schema: "Postulant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypePostulantInscription", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "University",
                schema: "Info",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Acronym = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_University", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FirstNameFather = table.Column<string>(type: "text", nullable: false),
                    FirstNameMother = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Document = table.Column<string>(type: "text", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    Password = table.Column<string>(type: "text", nullable: true),
                    Genero = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    IsDisabled = table.Column<string>(type: "text", nullable: true),
                    Birthdate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                schema: "Ubigeo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Department_Country_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "Ubigeo",
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentIssued",
                schema: "DocumentaryManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Correlative = table.Column<int>(type: "integer", nullable: false),
                    CorrelativeDisplay = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    PostulantId = table.Column<Guid>(type: "uuid", nullable: true),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    WatermarkText = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    StoragePath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentIssued", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentIssued_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "DocumentaryManagement",
                        principalTable: "DocumentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApiQueryLog",
                schema: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    RequestParametersJson = table.Column<string>(type: "text", nullable: true),
                    ResponseStatus = table.Column<int>(type: "integer", nullable: false),
                    ResponseSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ResponseExcerpt = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    QueriedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiQueryLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiQueryLog_ExternalApi_ApiId",
                        column: x => x.ApiId,
                        principalSchema: "Integrations",
                        principalTable: "ExternalApi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Career",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    TematicArea = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    BannerUrl = table.Column<string>(type: "text", nullable: true),
                    Initials = table.Column<string>(type: "text", nullable: true),
                    StudyPlanUrl = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    DurationSemesters = table.Column<int>(type: "integer", nullable: true),
                    DegreeTitle = table.Column<string>(type: "text", nullable: true),
                    AcademicDegree = table.Column<string>(type: "text", nullable: true),
                    GraduateProfile = table.Column<string>(type: "text", nullable: true),
                    AdmissionProfile = table.Column<string>(type: "text", nullable: true),
                    JobField = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Career", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Career_Faculty_FacultyId",
                        column: x => x.FacultyId,
                        principalSchema: "Modality",
                        principalTable: "Faculty",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Clasroom",
                schema: "Infrastructure",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Floor = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PavilionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clasroom", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clasroom_Pavilion_PavilionId",
                        column: x => x.PavilionId,
                        principalSchema: "Infrastructure",
                        principalTable: "Pavilion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Beneficiarie",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PercentageDiscount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficiarie", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beneficiarie_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Modality",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PublicSummary = table.Column<string>(type: "text", nullable: true),
                    IconKey = table.Column<string>(type: "text", nullable: true),
                    Badge = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresProfilePhoto = table.Column<bool>(type: "boolean", nullable: false),
                    IsMockExam = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresSchoolType = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresEducationalLevel = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresGrade = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExamDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ResultsPublicationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartingCode = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modality", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modality_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentCode",
                schema: "EconomicManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentCode_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prospect",
                schema: "Info",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prospect", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prospect_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleEvent",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Schedule = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleEvent_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TypePostulantRequisite",
                schema: "Requirement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TypePostulantInscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileRequirementManagementId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypePostulantRequisite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TypePostulantRequisite_FileRequirementManagement_FileRequir~",
                        column: x => x.FileRequirementManagementId,
                        principalSchema: "Requirement",
                        principalTable: "FileRequirementManagement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TypePostulantRequisite_TypePostulantInscription_TypePostula~",
                        column: x => x.TypePostulantInscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "TypePostulantInscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationView",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationView", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationView_Notification_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "Notifications",
                        principalTable: "Notification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationView_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Users",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Observations",
                schema: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Observation = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observations_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Users",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Postulant",
                schema: "Postulant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    DisableDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DisableReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    ConadisNumber = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Postulant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Postulant_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Users",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                schema: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Specialization = table.Column<string>(type: "text", nullable: false),
                    Degree = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teachers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Users",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRol",
                schema: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolsId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRol", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRol_Rols_RolsId",
                        column: x => x.RolsId,
                        principalSchema: "Users",
                        principalTable: "Rols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRol_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Users",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Province",
                schema: "Ubigeo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Province", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Province_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "Ubigeo",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAcademicInfo",
                schema: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalApiId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dni = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PaternalSurname = table.Column<string>(type: "text", nullable: false),
                    MaternalSurname = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    PersonalEmail = table.Column<string>(type: "text", nullable: true),
                    CareerName = table.Column<string>(type: "text", nullable: false),
                    FacultyName = table.Column<string>(type: "text", nullable: false),
                    TotalCreditsApproved = table.Column<decimal>(type: "numeric", nullable: false),
                    QueryLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    QueriedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAcademicInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalAcademicInfo_ApiQueryLog_QueryLogId",
                        column: x => x.QueryLogId,
                        principalSchema: "Integrations",
                        principalTable: "ApiQueryLog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalAcademicInfo_ExternalApi_ExternalApiId",
                        column: x => x.ExternalApiId,
                        principalSchema: "Integrations",
                        principalTable: "ExternalApi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalPaymentVoucher",
                schema: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalApiId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialVoucher = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    QueryLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    QueriedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalPaymentVoucher", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalPaymentVoucher_ApiQueryLog_QueryLogId",
                        column: x => x.QueryLogId,
                        principalSchema: "Integrations",
                        principalTable: "ApiQueryLog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalPaymentVoucher_ExternalApi_ExternalApiId",
                        column: x => x.ExternalApiId,
                        principalSchema: "Integrations",
                        principalTable: "ExternalApi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareerImage",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    Caption = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerImage_Career_CareerId",
                        column: x => x.CareerId,
                        principalSchema: "Modality",
                        principalTable: "Career",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TematicAreaCareer",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TematicAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TematicAreaCareer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TematicAreaCareer_Career_CareerId",
                        column: x => x.CareerId,
                        principalSchema: "Modality",
                        principalTable: "Career",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TematicAreaCareer_TematicArea_TematicAreaId",
                        column: x => x.TematicAreaId,
                        principalSchema: "Modality",
                        principalTable: "TematicArea",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TematicAreaCareer_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamResult",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamResult_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamResult_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamSession",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSession_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSession_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModalityCareer",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModalityCareer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModalityCareer_Career_CareerId",
                        column: x => x.CareerId,
                        principalSchema: "Modality",
                        principalTable: "Career",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModalityCareer_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicInfo",
                schema: "Info",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicInfo_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PublicInfo_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TypeModality",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeModality", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TypeModality_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fingerprints",
                schema: "Biometrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostulantId = table.Column<Guid>(type: "uuid", nullable: true),
                    FingerIndex = table.Column<int>(type: "integer", nullable: false),
                    Template = table.Column<string>(type: "text", nullable: false),
                    ImageBase64 = table.Column<string>(type: "text", nullable: true),
                    DeviceIp = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fingerprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fingerprints_Postulant_PostulantId",
                        column: x => x.PostulantId,
                        principalSchema: "Postulant",
                        principalTable: "Postulant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostulantDisabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostulantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisabilityTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostulantDisabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostulantDisabilities_DisabilityTypes_DisabilityTypeId",
                        column: x => x.DisabilityTypeId,
                        principalTable: "DisabilityTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostulantDisabilities_Postulant_PostulantId",
                        column: x => x.PostulantId,
                        principalSchema: "Postulant",
                        principalTable: "Postulant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostulantPhoto",
                schema: "Postulant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostulantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoUrl = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    PostulantId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostulantPhoto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostulantPhoto_Postulant_PostulantId",
                        column: x => x.PostulantId,
                        principalSchema: "Postulant",
                        principalTable: "Postulant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostulantPhoto_Postulant_PostulantId1",
                        column: x => x.PostulantId1,
                        principalSchema: "Postulant",
                        principalTable: "Postulant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Distrit",
                schema: "Ubigeo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    ProvinceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distrit", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Distrit_Province_ProvinceId",
                        column: x => x.ProvinceId,
                        principalSchema: "Ubigeo",
                        principalTable: "Province",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalPaymentDetail",
                schema: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    TypeUser = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsBankPayment = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ActiveDependency = table.Column<bool>(type: "boolean", nullable: false),
                    Acronym = table.Column<string>(type: "text", nullable: true),
                    Cashier = table.Column<string>(type: "text", nullable: true),
                    TermName = table.Column<string>(type: "text", nullable: true),
                    AmountInWords = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalPaymentDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalPaymentDetail_ExternalPaymentVoucher_VoucherId",
                        column: x => x.VoucherId,
                        principalSchema: "Integrations",
                        principalTable: "ExternalPaymentVoucher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamAnswerKey",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TematicAreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tema = table.Column<string>(type: "text", nullable: false),
                    NumeroPregunta = table.Column<int>(type: "integer", nullable: false),
                    RespuestaCorrecta = table.Column<string>(type: "text", nullable: false),
                    IsAnulada = table.Column<bool>(type: "boolean", nullable: false),
                    PuntosOverride = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAnswerKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAnswerKey_ExamSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Exam",
                        principalTable: "ExamSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAnswerKey_TematicArea_TematicAreaId",
                        column: x => x.TematicAreaId,
                        principalSchema: "Modality",
                        principalTable: "TematicArea",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamAreaConfig",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TematicAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroInicio = table.Column<int>(type: "integer", nullable: false),
                    NumeroFin = table.Column<int>(type: "integer", nullable: false),
                    PesoRelativo = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAreaConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAreaConfig_ExamSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Exam",
                        principalTable: "ExamSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAreaConfig_TematicArea_TematicAreaId",
                        column: x => x.TematicAreaId,
                        principalSchema: "Modality",
                        principalTable: "TematicArea",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamParameters",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PuntosCorrecta = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    PuntosBlanco = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    PuntosIncorrecta = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    TotalPreguntas = table.Column<int>(type: "integer", nullable: false),
                    NotaMinimaIngreso = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    AplicarVigesimal = table.Column<bool>(type: "boolean", nullable: false),
                    AplicarBonificacion = table.Column<bool>(type: "boolean", nullable: false),
                    ManejoAnuladas = table.Column<string>(type: "text", nullable: false),
                    CriterioDesempate = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamParameters_ExamSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Exam",
                        principalTable: "ExamSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModalityRequisite",
                schema: "Requirement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeModalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileRequirementManagementId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModalityRequisite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModalityRequisite_FileRequirementManagement_FileRequirement~",
                        column: x => x.FileRequirementManagementId,
                        principalSchema: "Requirement",
                        principalTable: "FileRequirementManagement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModalityRequisite_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModalityRequisite_TypeModality_TypeModalityId",
                        column: x => x.TypeModalityId,
                        principalSchema: "Modality",
                        principalTable: "TypeModality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentCodeModality",
                schema: "EconomicManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    TypeModalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentCodeModality", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentCodeModality_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentCodeModality_PaymentCode_PaymentCodeId",
                        column: x => x.PaymentCodeId,
                        principalSchema: "EconomicManagement",
                        principalTable: "PaymentCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentCodeModality_TypeModality_TypeModalityId",
                        column: x => x.TypeModalityId,
                        principalSchema: "Modality",
                        principalTable: "TypeModality",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TypeModalityCareer",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeModalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeModalityCareer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TypeModalityCareer_Career_CareerId",
                        column: x => x.CareerId,
                        principalSchema: "Modality",
                        principalTable: "Career",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TypeModalityCareer_TypeModality_TypeModalityId",
                        column: x => x.TypeModalityId,
                        principalSchema: "Modality",
                        principalTable: "TypeModality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vacancies",
                schema: "Modality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeModalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Available = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vacancies_Career_CareerId",
                        column: x => x.CareerId,
                        principalSchema: "Modality",
                        principalTable: "Career",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacancies_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vacancies_TypeModality_TypeModalityId",
                        column: x => x.TypeModalityId,
                        principalSchema: "Modality",
                        principalTable: "TypeModality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Schools",
                schema: "Schools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    UgelName = table.Column<string>(type: "text", nullable: true),
                    Modality = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<string>(type: "text", nullable: true),
                    Management = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Director = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DistritId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schools_Distrit_DistritId",
                        column: x => x.DistritId,
                        principalSchema: "Ubigeo",
                        principalTable: "Distrit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Inscription",
                schema: "Postulant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodePostulant = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    IsAdmission = table.Column<bool>(type: "boolean", nullable: false),
                    GradeAdmission = table.Column<decimal>(type: "numeric", nullable: true),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    DistritId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostulantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeModalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    TypePostulantInscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    OtherSchool = table.Column<string>(type: "text", nullable: true),
                    DJ = table.Column<bool>(type: "boolean", nullable: false),
                    SchoolType = table.Column<string>(type: "text", nullable: true),
                    EducationalLevel = table.Column<string>(type: "text", nullable: true),
                    Grade = table.Column<string>(type: "text", nullable: true),
                    SourceUniversityId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceCareerName = table.Column<string>(type: "text", nullable: true),
                    SourceCareerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inscription_Career_CareerId",
                        column: x => x.CareerId,
                        principalSchema: "Modality",
                        principalTable: "Career",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscription_Career_SourceCareerId",
                        column: x => x.SourceCareerId,
                        principalSchema: "Modality",
                        principalTable: "Career",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscription_Country_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "Ubigeo",
                        principalTable: "Country",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscription_Distrit_DistritId",
                        column: x => x.DistritId,
                        principalSchema: "Ubigeo",
                        principalTable: "Distrit",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscription_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscription_Postulant_PostulantId",
                        column: x => x.PostulantId,
                        principalSchema: "Postulant",
                        principalTable: "Postulant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscription_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalSchema: "Schools",
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscription_TypeModality_TypeModalityId",
                        column: x => x.TypeModalityId,
                        principalSchema: "Modality",
                        principalTable: "TypeModality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscription_TypePostulantInscription_TypePostulantInscripti~",
                        column: x => x.TypePostulantInscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "TypePostulantInscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscription_University_SourceUniversityId",
                        column: x => x.SourceUniversityId,
                        principalSchema: "Info",
                        principalTable: "University",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamAssignment",
                schema: "Infrastructure",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TematicAreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeatNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAssignment_Clasroom_ClassroomId",
                        column: x => x.ClassroomId,
                        principalSchema: "Infrastructure",
                        principalTable: "Clasroom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamAssignment_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAssignment_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamAssignment_TematicArea_TematicAreaId",
                        column: x => x.TematicAreaId,
                        principalSchema: "Modality",
                        principalTable: "TematicArea",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExamAssignment_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileSubmission",
                schema: "Postulant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileRequirementManagementId = table.Column<Guid>(type: "uuid", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<string>(type: "text", nullable: false),
                    IsValidated = table.Column<bool>(type: "boolean", nullable: false),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ValidatedBy = table.Column<string>(type: "text", nullable: true),
                    ValidationNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileSubmission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileSubmission_FileRequirementManagement_FileRequirementMan~",
                        column: x => x.FileRequirementManagementId,
                        principalSchema: "Requirement",
                        principalTable: "FileRequirementManagement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileSubmission_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Observations",
                schema: "Postulant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Observation = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observations_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Parent",
                schema: "Postulant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostulantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FirstNameFather = table.Column<string>(type: "text", nullable: false),
                    FirstNameMother = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    TypeDocument = table.Column<string>(type: "text", nullable: false),
                    NumberDocument = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parent_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Parent_Postulant_PostulantId",
                        column: x => x.PostulantId,
                        principalSchema: "Postulant",
                        principalTable: "Postulant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "EconomicManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationCode = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: true),
                    MethodPaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    Observation = table.Column<string>(type: "text", nullable: true),
                    DatePayment = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    ExternalPaymentVoucherId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_ExternalPaymentVoucher_ExternalPaymentVoucherId",
                        column: x => x.ExternalPaymentVoucherId,
                        principalSchema: "Integrations",
                        principalTable: "ExternalPaymentVoucher",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Payments_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Payments_MethodPayments_MethodPaymentId",
                        column: x => x.MethodPaymentId,
                        principalSchema: "EconomicManagement",
                        principalTable: "MethodPayments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PostulantAnswerSheet",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodePostulant = table.Column<string>(type: "text", nullable: false),
                    Tema = table.Column<string>(type: "text", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileRowNumber = table.Column<int>(type: "integer", nullable: false),
                    HasIssues = table.Column<bool>(type: "boolean", nullable: false),
                    IssueMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostulantAnswerSheet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostulantAnswerSheet_ExamSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Exam",
                        principalTable: "ExamSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostulantAnswerSheet_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PostulantAttendance",
                schema: "Biometrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BiometricStatus = table.Column<string>(type: "text", nullable: false),
                    BiometricScore = table.Column<int>(type: "integer", nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VerifiedBy = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostulantAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostulantAttendance_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Resignation",
                schema: "Postulant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateResignation = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    File = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resignation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resignation_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamScoreResult",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Correctas = table.Column<int>(type: "integer", nullable: false),
                    Incorrectas = table.Column<int>(type: "integer", nullable: false),
                    Blancas = table.Column<int>(type: "integer", nullable: false),
                    Anuladas = table.Column<int>(type: "integer", nullable: false),
                    Multiples = table.Column<int>(type: "integer", nullable: false),
                    PuntajeBruto = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    PuntajeFinal = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    Vigesimal = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true),
                    AreaScoresJson = table.Column<string>(type: "text", nullable: true),
                    RankingCarrera = table.Column<int>(type: "integer", nullable: true),
                    RankingModalidad = table.Column<int>(type: "integer", nullable: true),
                    EsIngresante = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamScoreResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamScoreResult_ExamSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Exam",
                        principalTable: "ExamSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamScoreResult_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamScoreResult_PostulantAnswerSheet_SheetId",
                        column: x => x.SheetId,
                        principalSchema: "Exam",
                        principalTable: "PostulantAnswerSheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostulantAnswer",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroPregunta = table.Column<int>(type: "integer", nullable: false),
                    RespuestaMarcada = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostulantAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostulantAnswer_PostulantAnswerSheet_SheetId",
                        column: x => x.SheetId,
                        principalSchema: "Exam",
                        principalTable: "PostulantAnswerSheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYearName_IsActive",
                schema: "DocumentaryManagement",
                table: "AcademicYearName",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYearName_Year",
                schema: "DocumentaryManagement",
                table: "AcademicYearName",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_ApiQueryLog_ApiId",
                schema: "Integrations",
                table: "ApiQueryLog",
                column: "ApiId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiQueryLog_QueriedAt",
                schema: "Integrations",
                table: "ApiQueryLog",
                column: "QueriedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiarie_TermId",
                schema: "Modality",
                table: "Beneficiarie",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_Career_FacultyId",
                schema: "Modality",
                table: "Career",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerImage_CareerId_DisplayOrder",
                schema: "Modality",
                table: "CareerImage",
                columns: new[] { "CareerId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Clasroom_PavilionId",
                schema: "Infrastructure",
                table: "Clasroom",
                column: "PavilionId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_CountryId",
                schema: "Ubigeo",
                table: "Department",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Distrit_ProvinceId",
                schema: "Ubigeo",
                table: "Distrit",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIssued_DocumentTypeId_Year_Correlative",
                schema: "DocumentaryManagement",
                table: "DocumentIssued",
                columns: new[] { "DocumentTypeId", "Year", "Correlative" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIssued_PostulantId",
                schema: "DocumentaryManagement",
                table: "DocumentIssued",
                column: "PostulantId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentType_Code",
                schema: "DocumentaryManagement",
                table: "DocumentType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentType_TemplateName",
                schema: "DocumentaryManagement",
                table: "DocumentType",
                column: "TemplateName");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerKey_SessionId_Tema_NumeroPregunta",
                schema: "Exam",
                table: "ExamAnswerKey",
                columns: new[] { "SessionId", "Tema", "NumeroPregunta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerKey_TematicAreaId",
                schema: "Exam",
                table: "ExamAnswerKey",
                column: "TematicAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAreaConfig_SessionId",
                schema: "Exam",
                table: "ExamAreaConfig",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAreaConfig_TematicAreaId",
                schema: "Exam",
                table: "ExamAreaConfig",
                column: "TematicAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignment_ClassroomId",
                schema: "Infrastructure",
                table: "ExamAssignment",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignment_InscriptionId",
                schema: "Infrastructure",
                table: "ExamAssignment",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignment_ModalityId_InscriptionId",
                schema: "Infrastructure",
                table: "ExamAssignment",
                columns: new[] { "ModalityId", "InscriptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignment_TematicAreaId",
                schema: "Infrastructure",
                table: "ExamAssignment",
                column: "TematicAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignment_TermId",
                schema: "Infrastructure",
                table: "ExamAssignment",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamParameters_SessionId",
                schema: "Exam",
                table: "ExamParameters",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamResult_ModalityId",
                schema: "Modality",
                table: "ExamResult",
                column: "ModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamResult_TermId",
                schema: "Modality",
                table: "ExamResult",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScoreResult_InscriptionId",
                schema: "Exam",
                table: "ExamScoreResult",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScoreResult_SessionId_EsIngresante",
                schema: "Exam",
                table: "ExamScoreResult",
                columns: new[] { "SessionId", "EsIngresante" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamScoreResult_SheetId",
                schema: "Exam",
                table: "ExamScoreResult",
                column: "SheetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSession_ModalityId",
                schema: "Exam",
                table: "ExamSession",
                column: "ModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSession_TermId",
                schema: "Exam",
                table: "ExamSession",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAcademicInfo_Dni",
                schema: "Integrations",
                table: "ExternalAcademicInfo",
                column: "Dni");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAcademicInfo_ExternalApiId",
                schema: "Integrations",
                table: "ExternalAcademicInfo",
                column: "ExternalApiId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAcademicInfo_QueryLogId",
                schema: "Integrations",
                table: "ExternalAcademicInfo",
                column: "QueryLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalApi_Name",
                schema: "Integrations",
                table: "ExternalApi",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentDetail_VoucherId",
                schema: "Integrations",
                table: "ExternalPaymentDetail",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentVoucher_ExternalApiId",
                schema: "Integrations",
                table: "ExternalPaymentVoucher",
                column: "ExternalApiId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentVoucher_QueryLogId",
                schema: "Integrations",
                table: "ExternalPaymentVoucher",
                column: "QueryLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalPaymentVoucher_UserName",
                schema: "Integrations",
                table: "ExternalPaymentVoucher",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_FaqItem_ParentId",
                schema: "Info",
                table: "FaqItem",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_FileSubmission_FileRequirementManagementId",
                schema: "Postulant",
                table: "FileSubmission",
                column: "FileRequirementManagementId");

            migrationBuilder.CreateIndex(
                name: "IX_FileSubmission_InscriptionId",
                schema: "Postulant",
                table: "FileSubmission",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Fingerprints_PostulantId",
                schema: "Biometrics",
                table: "Fingerprints",
                column: "PostulantId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_CareerId",
                schema: "Postulant",
                table: "Inscription",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_CountryId",
                schema: "Postulant",
                table: "Inscription",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_DistritId",
                schema: "Postulant",
                table: "Inscription",
                column: "DistritId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_ModalityId",
                schema: "Postulant",
                table: "Inscription",
                column: "ModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_PostulantId",
                schema: "Postulant",
                table: "Inscription",
                column: "PostulantId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_SchoolId",
                schema: "Postulant",
                table: "Inscription",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_SourceCareerId",
                schema: "Postulant",
                table: "Inscription",
                column: "SourceCareerId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_SourceUniversityId",
                schema: "Postulant",
                table: "Inscription",
                column: "SourceUniversityId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_TypeModalityId",
                schema: "Postulant",
                table: "Inscription",
                column: "TypeModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_TypePostulantInscriptionId",
                schema: "Postulant",
                table: "Inscription",
                column: "TypePostulantInscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Modality_TermId",
                schema: "Modality",
                table: "Modality",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_ModalityCareer_CareerId",
                schema: "Modality",
                table: "ModalityCareer",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_ModalityCareer_ModalityId_CareerId",
                schema: "Modality",
                table: "ModalityCareer",
                columns: new[] { "ModalityId", "CareerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModalityRequisite_FileRequirementManagementId",
                schema: "Requirement",
                table: "ModalityRequisite",
                column: "FileRequirementManagementId");

            migrationBuilder.CreateIndex(
                name: "IX_ModalityRequisite_ModalityId",
                schema: "Requirement",
                table: "ModalityRequisite",
                column: "ModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_ModalityRequisite_TypeModalityId",
                schema: "Requirement",
                table: "ModalityRequisite",
                column: "TypeModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_CreatedAt",
                schema: "Notifications",
                table: "Notification",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_EntityType_EntityId",
                schema: "Notifications",
                table: "Notification",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationView_NotificationId_UserId",
                schema: "Notifications",
                table: "NotificationView",
                columns: new[] { "NotificationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationView_UserId_ViewedAt",
                schema: "Notifications",
                table: "NotificationView",
                columns: new[] { "UserId", "ViewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Observations_InscriptionId",
                schema: "Postulant",
                table: "Observations",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_UserId",
                schema: "Users",
                table: "Observations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Parent_InscriptionId",
                schema: "Postulant",
                table: "Parent",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Parent_PostulantId",
                schema: "Postulant",
                table: "Parent",
                column: "PostulantId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCode_TermId",
                schema: "EconomicManagement",
                table: "PaymentCode",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCodeModality_ModalityId",
                schema: "EconomicManagement",
                table: "PaymentCodeModality",
                column: "ModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCodeModality_PaymentCodeId",
                schema: "EconomicManagement",
                table: "PaymentCodeModality",
                column: "PaymentCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentCodeModality_TypeModalityId",
                schema: "EconomicManagement",
                table: "PaymentCodeModality",
                column: "TypeModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ExternalPaymentVoucherId",
                schema: "EconomicManagement",
                table: "Payments",
                column: "ExternalPaymentVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InscriptionId",
                schema: "EconomicManagement",
                table: "Payments",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MethodPaymentId",
                schema: "EconomicManagement",
                table: "Payments",
                column: "MethodPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OperationCode",
                schema: "EconomicManagement",
                table: "Payments",
                column: "OperationCode");

            migrationBuilder.CreateIndex(
                name: "IX_Postulant_UserId",
                schema: "Postulant",
                table: "Postulant",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostulantAnswer_SheetId_NumeroPregunta",
                schema: "Exam",
                table: "PostulantAnswer",
                columns: new[] { "SheetId", "NumeroPregunta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostulantAnswerSheet_InscriptionId",
                schema: "Exam",
                table: "PostulantAnswerSheet",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PostulantAnswerSheet_SessionId",
                schema: "Exam",
                table: "PostulantAnswerSheet",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PostulantAttendance_InscriptionId",
                schema: "Biometrics",
                table: "PostulantAttendance",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PostulantDisabilities_DisabilityTypeId",
                table: "PostulantDisabilities",
                column: "DisabilityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PostulantDisabilities_PostulantId",
                table: "PostulantDisabilities",
                column: "PostulantId");

            migrationBuilder.CreateIndex(
                name: "IX_PostulantPhoto_PostulantId",
                schema: "Postulant",
                table: "PostulantPhoto",
                column: "PostulantId");

            migrationBuilder.CreateIndex(
                name: "IX_PostulantPhoto_PostulantId1",
                schema: "Postulant",
                table: "PostulantPhoto",
                column: "PostulantId1");

            migrationBuilder.CreateIndex(
                name: "IX_Prospect_TermId",
                schema: "Info",
                table: "Prospect",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_Province_DepartmentId",
                schema: "Ubigeo",
                table: "Province",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicInfo_ModalityId",
                schema: "Info",
                table: "PublicInfo",
                column: "ModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicInfo_TermId",
                schema: "Info",
                table: "PublicInfo",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_Resignation_InscriptionId",
                schema: "Postulant",
                table: "Resignation",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEvent_TermId_Phase_DisplayOrder",
                schema: "Modality",
                table: "ScheduleEvent",
                columns: new[] { "TermId", "Phase", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Schools_DistritId",
                schema: "Schools",
                table: "Schools",
                column: "DistritId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_UserId",
                schema: "Users",
                table: "Teachers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TematicAreaCareer_CareerId",
                schema: "Modality",
                table: "TematicAreaCareer",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_TematicAreaCareer_TematicAreaId",
                schema: "Modality",
                table: "TematicAreaCareer",
                column: "TematicAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_TematicAreaCareer_TermId",
                schema: "Modality",
                table: "TematicAreaCareer",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_TypeModality_ModalityId",
                schema: "Modality",
                table: "TypeModality",
                column: "ModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_TypeModalityCareer_CareerId",
                schema: "Modality",
                table: "TypeModalityCareer",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_TypeModalityCareer_TypeModalityId_CareerId",
                schema: "Modality",
                table: "TypeModalityCareer",
                columns: new[] { "TypeModalityId", "CareerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TypePostulantRequisite_FileRequirementManagementId",
                schema: "Requirement",
                table: "TypePostulantRequisite",
                column: "FileRequirementManagementId");

            migrationBuilder.CreateIndex(
                name: "IX_TypePostulantRequisite_TypePostulantInscriptionId",
                schema: "Requirement",
                table: "TypePostulantRequisite",
                column: "TypePostulantInscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRol_RolsId",
                schema: "Users",
                table: "UserRol",
                column: "RolsId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRol_UserId",
                schema: "Users",
                table: "UserRol",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_CareerId",
                schema: "Modality",
                table: "Vacancies",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_ModalityId",
                schema: "Modality",
                table: "Vacancies",
                column: "ModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_TypeModalityId",
                schema: "Modality",
                table: "Vacancies",
                column: "TypeModalityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicYearName",
                schema: "DocumentaryManagement");

            migrationBuilder.DropTable(
                name: "AccessLog",
                schema: "System");

            migrationBuilder.DropTable(
                name: "Audit",
                schema: "System");

            migrationBuilder.DropTable(
                name: "Banner",
                schema: "Info");

            migrationBuilder.DropTable(
                name: "Beneficiarie",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "CareerImage",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "Config",
                schema: "System");

            migrationBuilder.DropTable(
                name: "DocumentHeaderConfig",
                schema: "DocumentaryManagement");

            migrationBuilder.DropTable(
                name: "DocumentIssued",
                schema: "DocumentaryManagement");

            migrationBuilder.DropTable(
                name: "ExamAnswerKey",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "ExamAreaConfig",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "ExamAssignment",
                schema: "Infrastructure");

            migrationBuilder.DropTable(
                name: "ExamParameters",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "ExamResult",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "ExamScoreResult",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "ExternalAcademicInfo",
                schema: "Integrations");

            migrationBuilder.DropTable(
                name: "ExternalPaymentDetail",
                schema: "Integrations");

            migrationBuilder.DropTable(
                name: "FaqItem",
                schema: "Info");

            migrationBuilder.DropTable(
                name: "FileSubmission",
                schema: "Postulant");

            migrationBuilder.DropTable(
                name: "Fingerprints",
                schema: "Biometrics");

            migrationBuilder.DropTable(
                name: "ModalityCareer",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "ModalityRequisite",
                schema: "Requirement");

            migrationBuilder.DropTable(
                name: "NotificationView",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "Observations",
                schema: "Postulant");

            migrationBuilder.DropTable(
                name: "Observations",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "OtherFiles",
                schema: "Info");

            migrationBuilder.DropTable(
                name: "Parent",
                schema: "Postulant");

            migrationBuilder.DropTable(
                name: "PaymentCodeModality",
                schema: "EconomicManagement");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "EconomicManagement");

            migrationBuilder.DropTable(
                name: "PostulantAnswer",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "PostulantAttendance",
                schema: "Biometrics");

            migrationBuilder.DropTable(
                name: "PostulantDisabilities");

            migrationBuilder.DropTable(
                name: "PostulantPhoto",
                schema: "Postulant");

            migrationBuilder.DropTable(
                name: "Prospect",
                schema: "Info");

            migrationBuilder.DropTable(
                name: "PublicInfo",
                schema: "Info");

            migrationBuilder.DropTable(
                name: "Resignation",
                schema: "Postulant");

            migrationBuilder.DropTable(
                name: "ScheduleEvent",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "Teachers",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "TematicAreaCareer",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "TypeModalityCareer",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "TypePostulantRequisite",
                schema: "Requirement");

            migrationBuilder.DropTable(
                name: "UserRol",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "Vacancies",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "DocumentType",
                schema: "DocumentaryManagement");

            migrationBuilder.DropTable(
                name: "Clasroom",
                schema: "Infrastructure");

            migrationBuilder.DropTable(
                name: "Notification",
                schema: "Notifications");

            migrationBuilder.DropTable(
                name: "PaymentCode",
                schema: "EconomicManagement");

            migrationBuilder.DropTable(
                name: "ExternalPaymentVoucher",
                schema: "Integrations");

            migrationBuilder.DropTable(
                name: "MethodPayments",
                schema: "EconomicManagement");

            migrationBuilder.DropTable(
                name: "PostulantAnswerSheet",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "DisabilityTypes");

            migrationBuilder.DropTable(
                name: "TematicArea",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "FileRequirementManagement",
                schema: "Requirement");

            migrationBuilder.DropTable(
                name: "Rols",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "Pavilion",
                schema: "Infrastructure");

            migrationBuilder.DropTable(
                name: "ApiQueryLog",
                schema: "Integrations");

            migrationBuilder.DropTable(
                name: "ExamSession",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "Inscription",
                schema: "Postulant");

            migrationBuilder.DropTable(
                name: "ExternalApi",
                schema: "Integrations");

            migrationBuilder.DropTable(
                name: "Career",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "Postulant",
                schema: "Postulant");

            migrationBuilder.DropTable(
                name: "Schools",
                schema: "Schools");

            migrationBuilder.DropTable(
                name: "TypeModality",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "TypePostulantInscription",
                schema: "Postulant");

            migrationBuilder.DropTable(
                name: "University",
                schema: "Info");

            migrationBuilder.DropTable(
                name: "Faculty",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "Distrit",
                schema: "Ubigeo");

            migrationBuilder.DropTable(
                name: "Modality",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "Province",
                schema: "Ubigeo");

            migrationBuilder.DropTable(
                name: "Terms",
                schema: "Modality");

            migrationBuilder.DropTable(
                name: "Department",
                schema: "Ubigeo");

            migrationBuilder.DropTable(
                name: "Country",
                schema: "Ubigeo");
        }
    }
}
