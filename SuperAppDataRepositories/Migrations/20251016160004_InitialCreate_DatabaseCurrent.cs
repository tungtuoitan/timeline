using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperAppDataRepositories.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_DatabaseCurrent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "entity_types",
                columns: table => new
                {
                    type_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    display_plural = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    table_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    supports_versioning = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    supports_sharing = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_types", x => x.type_name);
                });

            migrationBuilder.CreateTable(
                name: "standard_registry",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    active = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_standard_registry", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    avatar_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    bio = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    preferences = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    email_verified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                columns: table => new
                {
                    note_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    is_archived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_pinned = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_favorite = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    word_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    version_count = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.note_id);
                    table.ForeignKey(
                        name: "FK_notes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    tag_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true, defaultValue: "#3B82F6"),
                    icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    usage_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.tag_id);
                    table.ForeignKey(
                        name: "FK_tags_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    workspace_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "hierarchy"),
                    max_depth = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    is_default = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_public = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_template = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_archived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    tag_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    relationship_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    member_count = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    settings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_accessed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspaces", x => x.workspace_id);
                    table.ForeignKey(
                        name: "FK_workspaces_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "note_members",
                columns: table => new
                {
                    member_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    note_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "viewer"),
                    invited_by = table.Column<int>(type: "int", nullable: true),
                    invitation_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "active"),
                    invited_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    joined_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_accessed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_members", x => x.member_id);
                    table.ForeignKey(
                        name: "FK_note_members_notes_note_id",
                        column: x => x.note_id,
                        principalTable: "notes",
                        principalColumn: "note_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_note_members_users_invited_by",
                        column: x => x.invited_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_note_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "note_versions",
                columns: table => new
                {
                    version_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    note_id = table.Column<int>(type: "int", nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    word_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    change_summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_versions", x => x.version_id);
                    table.ForeignKey(
                        name: "FK_note_versions_notes_note_id",
                        column: x => x.note_id,
                        principalTable: "notes",
                        principalColumn: "note_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_note_versions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "note_tags",
                columns: table => new
                {
                    note_id = table.Column<int>(type: "int", nullable: false),
                    tag_id = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_tags", x => new { x.note_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_note_tags_notes_note_id",
                        column: x => x.note_id,
                        principalTable: "notes",
                        principalColumn: "note_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_note_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_items",
                columns: table => new
                {
                    item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    workspace_id = table.Column<int>(type: "int", nullable: false),
                    parent_tag_id = table.Column<int>(type: "int", nullable: false),
                    child_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    child_id = table.Column<int>(type: "int", nullable: false),
                    relationship_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    label = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    item_path = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    depth = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    sort_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    added_by = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChildTagTagId = table.Column<int>(type: "int", nullable: true),
                    ChildNoteNoteId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_items", x => x.item_id);
                    table.ForeignKey(
                        name: "FK_workspace_items_notes_ChildNoteNoteId",
                        column: x => x.ChildNoteNoteId,
                        principalTable: "notes",
                        principalColumn: "note_id");
                    table.ForeignKey(
                        name: "FK_workspace_items_tags_ChildTagTagId",
                        column: x => x.ChildTagTagId,
                        principalTable: "tags",
                        principalColumn: "tag_id");
                    table.ForeignKey(
                        name: "FK_workspace_items_tags_parent_tag_id",
                        column: x => x.parent_tag_id,
                        principalTable: "tags",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workspace_items_users_added_by",
                        column: x => x.added_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workspace_items_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "workspace_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_members",
                columns: table => new
                {
                    member_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    workspace_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "viewer"),
                    invited_by = table.Column<int>(type: "int", nullable: true),
                    invitation_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "active"),
                    custom_permissions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    invited_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    joined_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_accessed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_members", x => x.member_id);
                    table.ForeignKey(
                        name: "FK_workspace_members_users_invited_by",
                        column: x => x.invited_by,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workspace_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workspace_members_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "workspace_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_relationship_types",
                columns: table => new
                {
                    relationship_type_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    workspace_id = table.Column<int>(type: "int", nullable: false),
                    type_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    line_style = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "solid"),
                    line_width = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    is_bidirectional = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    allows_cycles = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    max_depth = table.Column<int>(type: "int", nullable: true),
                    validation_rules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_relationship_types", x => x.relationship_type_id);
                    table.ForeignKey(
                        name: "FK_workspace_relationship_types_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "workspace_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_entity_types_display_name",
                table: "entity_types",
                column: "display_name");

            migrationBuilder.CreateIndex(
                name: "ix_entity_types_enabled",
                table: "entity_types",
                column: "is_enabled",
                filter: "[is_enabled] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_note_members_invited_by",
                table: "note_members",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "IX_note_members_note_id",
                table: "note_members",
                column: "note_id");

            migrationBuilder.CreateIndex(
                name: "IX_note_members_note_user",
                table: "note_members",
                columns: new[] { "note_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_note_members_role",
                table: "note_members",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_note_members_user_id",
                table: "note_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_note_tags_created",
                table: "note_tags",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_note_tags_note",
                table: "note_tags",
                column: "note_id");

            migrationBuilder.CreateIndex(
                name: "ix_note_tags_tag",
                table: "note_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_note_versions_created_at",
                table: "note_versions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_note_versions_created_by",
                table: "note_versions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_note_versions_note_id",
                table: "note_versions",
                column: "note_id");

            migrationBuilder.CreateIndex(
                name: "IX_note_versions_note_version",
                table: "note_versions",
                columns: new[] { "note_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notes_is_archived",
                table: "notes",
                column: "is_archived");

            migrationBuilder.CreateIndex(
                name: "IX_notes_is_favorite",
                table: "notes",
                column: "is_favorite");

            migrationBuilder.CreateIndex(
                name: "IX_notes_is_pinned",
                table: "notes",
                column: "is_pinned");

            migrationBuilder.CreateIndex(
                name: "IX_notes_name",
                table: "notes",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_notes_slug",
                table: "notes",
                column: "slug");

            migrationBuilder.CreateIndex(
                name: "IX_notes_user_id",
                table: "notes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_standard_registry_active",
                table: "standard_registry",
                column: "active",
                filter: "active = 1");

            migrationBuilder.CreateIndex(
                name: "ix_standard_registry_code",
                table: "standard_registry",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_standard_registry_type",
                table: "standard_registry",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_standard_registry_type_code",
                table: "standard_registry",
                columns: new[] { "type", "code" });

            migrationBuilder.CreateIndex(
                name: "IX_tags_created_at",
                table: "tags",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_tags_name",
                table: "tags",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_tags_usage_count",
                table: "tags",
                column: "usage_count");

            migrationBuilder.CreateIndex(
                name: "IX_tags_user",
                table: "tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ_tags_user_slug",
                table: "tags",
                columns: new[] { "user_id", "slug" },
                unique: true,
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_is_active",
                table: "users",
                column: "is_active",
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true,
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_items_added_by",
                table: "workspace_items",
                column: "added_by");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_items_child",
                table: "workspace_items",
                columns: new[] { "child_type", "child_id" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_items_ChildNoteNoteId",
                table: "workspace_items",
                column: "ChildNoteNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_items_ChildTagTagId",
                table: "workspace_items",
                column: "ChildTagTagId");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_items_parent",
                table: "workspace_items",
                column: "parent_tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_items_path",
                table: "workspace_items",
                column: "item_path");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_items_workspace",
                table: "workspace_items",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_items_workspace_parent",
                table: "workspace_items",
                columns: new[] { "workspace_id", "parent_tag_id" });

            migrationBuilder.CreateIndex(
                name: "UQ_workspace_items_unique",
                table: "workspace_items",
                columns: new[] { "workspace_id", "parent_tag_id", "child_type", "child_id" },
                unique: true,
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_invited_by",
                table: "workspace_members",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_role",
                table: "workspace_members",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_user",
                table: "workspace_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_members_workspace",
                table: "workspace_members",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "UQ_workspace_members_unique",
                table: "workspace_members",
                columns: new[] { "workspace_id", "user_id" },
                unique: true,
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_relationship_types_sort_order",
                table: "workspace_relationship_types",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_relationship_types_workspace",
                table: "workspace_relationship_types",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "UQ_workspace_relationship_types_unique",
                table: "workspace_relationship_types",
                columns: new[] { "workspace_id", "type_name" },
                unique: true,
                filter: "[deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_is_default",
                table: "workspaces",
                column: "is_default",
                filter: "[is_default] = 1 AND [deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_is_public",
                table: "workspaces",
                column: "is_public",
                filter: "[is_public] = 1 AND [deleted_at] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_last_accessed",
                table: "workspaces",
                column: "last_accessed_at");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_name",
                table: "workspaces",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_type",
                table: "workspaces",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_user",
                table: "workspaces",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity_types");

            migrationBuilder.DropTable(
                name: "note_members");

            migrationBuilder.DropTable(
                name: "note_tags");

            migrationBuilder.DropTable(
                name: "note_versions");

            migrationBuilder.DropTable(
                name: "standard_registry");

            migrationBuilder.DropTable(
                name: "workspace_items");

            migrationBuilder.DropTable(
                name: "workspace_members");

            migrationBuilder.DropTable(
                name: "workspace_relationship_types");

            migrationBuilder.DropTable(
                name: "notes");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "workspaces");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
