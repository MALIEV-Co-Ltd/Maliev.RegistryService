using System.Text.Json.Serialization;

namespace Maliev.RegistryService.Data.Models;

/// <summary>
/// Base response wrapper for BDEX API responses.
/// </summary>
internal sealed record BdexResponse<T>
{
    [JsonPropertyName("status")]
    public BdexStatus Status { get; init; } = new();

    [JsonPropertyName("data")]
    public T? Data { get; init; }
}

/// <summary>
/// Status information in BDEX API responses.
/// </summary>
internal sealed record BdexStatus
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Indicates if the API call was successful (code "1000").
    /// </summary>
    public bool IsSuccess => Code == "1000";
}

/// <summary>
/// OAuth token response data from BDEX API.
/// </summary>
internal sealed record BdexTokenData
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("tokenType")]
    public string TokenType { get; init; } = string.Empty;

    [JsonPropertyName("expiresIn")]
    public string ExpiresIn { get; init; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public string ExpiresAt { get; init; } = string.Empty;
}

/// <summary>
/// Request body for OAuth token endpoint.
/// </summary>
internal sealed record BdexTokenRequest
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; init; } = "client_credentials";
}

/// <summary>
/// Request body for company lookup by juristic ID.
/// </summary>
internal sealed record BdexCompanyLookupRequest
{
    [JsonPropertyName("OrganizationJuristicID")]
    public string OrganizationJuristicID { get; init; } = string.Empty;
}

/// <summary>
/// Company information returned by BDEX API.
/// Based on official DBD API documentation schema.
/// </summary>
internal sealed record BdexCompanyData
{
    /// <summary>
    /// 13-digit juristic registration ID (เลขทะเบียนนิติบุคคล 13 หลัก).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicID")]
    public string? OrganizationJuristicID { get; init; }

    /// <summary>
    /// Old juristic registration ID (เลขทะเบียนนิติบุคคลเดิม).
    /// </summary>
    [JsonPropertyName("OrganizationOldJuristicID")]
    public string? OrganizationOldJuristicID { get; init; }

    /// <summary>
    /// Company name in Thai (ชื่อนิติบุคคล ภาษาไทย).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicNameTH")]
    public string? OrganizationJuristicNameTH { get; init; }

    /// <summary>
    /// Company name in English (ชื่อนิติบุคคล ภาษาอังกฤษ).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicNameEN")]
    public string? OrganizationJuristicNameEN { get; init; }

    /// <summary>
    /// Type of juristic person (ประเภทนิติบุคคล).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicType")]
    public string? OrganizationJuristicType { get; init; }

    /// <summary>
    /// Registration date (วันที่จดทะเบียนจัดตั้งของนิติบุคคล).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicRegisterDate")]
    public string? OrganizationJuristicRegisterDate { get; init; }

    /// <summary>
    /// Status of juristic person (สถานะของนิติบุคคล).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicStatus")]
    public string? OrganizationJuristicStatus { get; init; }

    /// <summary>
    /// Business objectives array (วัตถุประสงค์ของนิติบุคคล).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicObjective")]
    public List<BdexJuristicObjective>? OrganizationJuristicObjective { get; init; }

    /// <summary>
    /// Number of objective items (จำนวนข้อวัตถุประสงค์).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicObjectiveItems")]
    public string? OrganizationJuristicObjectiveItems { get; init; }

    /// <summary>
    /// Number of pages (จำนวนแผ่น).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicObjectivePages")]
    public string? OrganizationJuristicObjectivePages { get; init; }

    /// <summary>
    /// Registered capital in Baht (ทุนจดทะเบียน บาท).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicRegisterCapital")]
    public string? OrganizationJuristicRegisterCapital { get; init; }

    /// <summary>
    /// Paid-up capital (ทุนเรียกชำระแล้ว).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicPaidUpCapital")]
    public string? OrganizationJuristicPaidUpCapital { get; init; }

    /// <summary>
    /// List of directors/partners (รายชื่อบุคคลที่เป็นกรรมการ/ผู้เป็นหุ้นส่วนของนิติบุคคล).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicPersonList")]
    public List<BdexJuristicPerson>? OrganizationJuristicPersonList { get; init; }

    /// <summary>
    /// Branch name (ชื่อสาขาของนิติบุคคล).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicBranchName")]
    public string? OrganizationJuristicBranchName { get; init; }

    /// <summary>
    /// Office address (ที่ตั้งของสำนักงานนิติบุคคล).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicAddress")]
    public BdexAddress? OrganizationJuristicAddress { get; init; }

    /// <summary>
    /// Other descriptions (ข้อมูลอื่น ๆ ของนิติบุคคล).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicPersonDescription")]
    public List<BdexPersonDescription>? OrganizationJuristicPersonDescription { get; init; }

    /// <summary>
    /// Digital ID support flag (รองรับการให้บริการ Digital ID).
    /// </summary>
    [JsonPropertyName("DigitalIDFlag")]
    public string? DigitalIDFlag { get; init; }
}

/// <summary>
/// Business objective information.
/// </summary>
internal sealed record BdexJuristicObjective
{
    /// <summary>
    /// Objective type: R=Registration objective, F=Latest financial statement objective.
    /// </summary>
    [JsonPropertyName("JuristicObjective")]
    public string? JuristicObjective { get; init; }

    /// <summary>
    /// TSIC code reference (รหัส TSIC).
    /// </summary>
    [JsonPropertyName("JuristicObjectiveCode")]
    public string? JuristicObjectiveCode { get; init; }

    /// <summary>
    /// Objective text in Thai (วัตถุประสงค์ ภาษาไทย).
    /// </summary>
    [JsonPropertyName("JuristicObjectiveTextTH")]
    public string? JuristicObjectiveTextTH { get; init; }

    /// <summary>
    /// Objective text in English (วัตถุประสงค์ ภาษาอังกฤษ).
    /// </summary>
    [JsonPropertyName("JuristicObjectiveTextEN")]
    public string? JuristicObjectiveTextEN { get; init; }
}

/// <summary>
/// Director/partner information.
/// </summary>
internal sealed record BdexJuristicPerson
{
    /// <summary>
    /// Sequence number (ลำดับ).
    /// </summary>
    [JsonPropertyName("JuristicPersonSequence")]
    public string? JuristicPersonSequence { get; init; }

    /// <summary>
    /// Person type: Director or Partner (กรรมการ/ผู้เป็นหุ้นส่วน).
    /// </summary>
    [JsonPropertyName("JuristicPersonType")]
    public string? JuristicPersonType { get; init; }

    /// <summary>
    /// Person details.
    /// </summary>
    [JsonPropertyName("JuristicPerson")]
    public BdexPerson? JuristicPerson { get; init; }

    /// <summary>
    /// Investment type for partnerships: "เงินสด", "ทรัพย์สิน", "แรงงาน".
    /// </summary>
    [JsonPropertyName("JuristicPersonInvestType")]
    public string? JuristicPersonInvestType { get; init; }

    /// <summary>
    /// Investment amount for partnerships.
    /// </summary>
    [JsonPropertyName("JuristicPersonInvestAmount")]
    public string? JuristicPersonInvestAmount { get; init; }
}

/// <summary>
/// Person information.
/// </summary>
internal sealed record BdexPerson
{
    /// <summary>
    /// Person details.
    /// </summary>
    [JsonPropertyName("Person")]
    public BdexPersonDetail? Person { get; init; }
}

/// <summary>
/// Detailed person information.
/// </summary>
internal sealed record BdexPersonDetail
{
    /// <summary>
    /// Name in Thai.
    /// </summary>
    [JsonPropertyName("PersonNameTH")]
    public BdexPersonNameTH? PersonNameTH { get; init; }
}

/// <summary>
/// Person name in Thai.
/// </summary>
internal sealed record BdexPersonNameTH
{
    /// <summary>
    /// Title prefix (คำนำหน้าชื่อ).
    /// </summary>
    [JsonPropertyName("PersonNameTitleTextTH")]
    public string? PersonNameTitleTextTH { get; init; }

    /// <summary>
    /// First name (ชื่อตัว).
    /// </summary>
    [JsonPropertyName("PersonFirstNameTH")]
    public string? PersonFirstNameTH { get; init; }

    /// <summary>
    /// Middle name (ชื่อรอง).
    /// </summary>
    [JsonPropertyName("PersonMiddleNameTH")]
    public string? PersonMiddleNameTH { get; init; }

    /// <summary>
    /// Last name (นามสกุล).
    /// </summary>
    [JsonPropertyName("PersonLastNameTH")]
    public string? PersonLastNameTH { get; init; }
}

/// <summary>
/// Address information.
/// </summary>
internal sealed record BdexAddress
{
    /// <summary>
    /// Address type.
    /// </summary>
    [JsonPropertyName("AddressType")]
    public string? AddressType { get; init; }

    /// <summary>
    /// Full address text (ที่อยู่).
    /// </summary>
    [JsonPropertyName("Address")]
    public string? Address { get; init; }

    /// <summary>
    /// Building name (ชื่อตึก/อาคาร).
    /// </summary>
    [JsonPropertyName("Building")]
    public string? Building { get; init; }

    /// <summary>
    /// Room number (เลขที่ห้อง).
    /// </summary>
    [JsonPropertyName("RoomNo")]
    public string? RoomNo { get; init; }

    /// <summary>
    /// Floor (ชั้นที่).
    /// </summary>
    [JsonPropertyName("Floor")]
    public string? Floor { get; init; }

    /// <summary>
    /// House/building number (เลขที่บ้านหรืออาคาร).
    /// </summary>
    [JsonPropertyName("AddressNo")]
    public string? AddressNo { get; init; }

    /// <summary>
    /// Moo/village number (หมู่ที่).
    /// </summary>
    [JsonPropertyName("Moo")]
    public string? Moo { get; init; }

    /// <summary>
    /// Yaek (แยก).
    /// </summary>
    [JsonPropertyName("Yaek")]
    public string? Yaek { get; init; }

    /// <summary>
    /// Soi (ซอย).
    /// </summary>
    [JsonPropertyName("Soi")]
    public string? Soi { get; init; }

    /// <summary>
    /// Trok (ตรอก).
    /// </summary>
    [JsonPropertyName("Trok")]
    public string? Trok { get; init; }

    /// <summary>
    /// Village name (หมู่บ้าน).
    /// </summary>
    [JsonPropertyName("Village")]
    public string? Village { get; init; }

    /// <summary>
    /// Road (ถนน).
    /// </summary>
    [JsonPropertyName("Road")]
    public string? Road { get; init; }

    /// <summary>
    /// Sub-district/Tambon information (ตำบล).
    /// </summary>
    [JsonPropertyName("CitySubDivision")]
    public BdexCitySubDivision? CitySubDivision { get; init; }

    /// <summary>
    /// District/Amphoe information (อำเภอ).
    /// </summary>
    [JsonPropertyName("City")]
    public BdexCity? City { get; init; }

    /// <summary>
    /// Province information (จังหวัด).
    /// </summary>
    [JsonPropertyName("CountrySubDivision")]
    public BdexCountrySubDivision? CountrySubDivision { get; init; }
}

/// <summary>
/// Sub-district (Tambon) information.
/// </summary>
internal sealed record BdexCitySubDivision
{
    /// <summary>
    /// Sub-district code (รหัสตำบล).
    /// </summary>
    [JsonPropertyName("CitySubDivisionCode")]
    public string? CitySubDivisionCode { get; init; }

    /// <summary>
    /// Sub-district name in Thai (ชื่อตำบล).
    /// </summary>
    [JsonPropertyName("CitySubDivisionTextTH")]
    public string? CitySubDivisionTextTH { get; init; }
}

/// <summary>
/// District (Amphoe) information.
/// </summary>
internal sealed record BdexCity
{
    /// <summary>
    /// District code (รหัสอำเภอ).
    /// </summary>
    [JsonPropertyName("CityCode")]
    public string? CityCode { get; init; }

    /// <summary>
    /// District name in Thai (ชื่อของอำเภอ).
    /// </summary>
    [JsonPropertyName("CityTextTH")]
    public string? CityTextTH { get; init; }
}

/// <summary>
/// Province information.
/// </summary>
internal sealed record BdexCountrySubDivision
{
    /// <summary>
    /// Province code (รหัสจังหวัด).
    /// </summary>
    [JsonPropertyName("CountrySubDivisionCode")]
    public string? CountrySubDivisionCode { get; init; }

    /// <summary>
    /// Province name in Thai (ชื่อจังหวัด).
    /// </summary>
    [JsonPropertyName("CountrySubDivisionTextTH")]
    public string? CountrySubDivisionTextTH { get; init; }
}

/// <summary>
/// Other descriptions about the juristic person.
/// </summary>
internal sealed record BdexPersonDescription
{
    /// <summary>
    /// Sequence number (ลำดับของข้อมูล).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicPersonDescriptionSequence")]
    public string? OrganizationJuristicPersonDescriptionSequence { get; init; }

    /// <summary>
    /// Description type: อำนาจกรรมการ, ข้อจำกัดอำนาจหุ้นส่วนผู้จัดการ, etc.
    /// </summary>
    [JsonPropertyName("OrganizationJuristicPersonDescriptionType")]
    public string? OrganizationJuristicPersonDescriptionType { get; init; }

    /// <summary>
    /// Description detail (รายละเอียดข้อมูล).
    /// </summary>
    [JsonPropertyName("OrganizationJuristicPersonDescriptionDetail")]
    public string? OrganizationJuristicPersonDescriptionDetail { get; init; }
}
