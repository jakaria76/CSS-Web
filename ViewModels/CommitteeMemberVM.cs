namespace CSS.ViewModels
{
    public class CommitteeMemberVM
    {
        public string Id { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? FullNameBn { get; set; }
        public string CommitteePosition { get; set; } = "";
        public string? ProfileImageSrc { get; set; }
        public string? District { get; set; }
        public string? BloodGroup { get; set; }
        public bool IsTopLevel { get; set; }
    }
}
