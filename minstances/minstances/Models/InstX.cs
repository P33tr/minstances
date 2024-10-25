
public class InstX
{
    public Instance[] instances { get; set; }
}

public class Instance
{
    public string id { get; set; }
    public string name { get; set; }
    public DateTime added_at { get; set; }
    public DateTime updated_at { get; set; }
    public DateTime checked_at { get; set; }
    public int uptime { get; set; }
    public bool up { get; set; }
    public bool dead { get; set; }
    public string version { get; set; }
    public bool ipv6 { get; set; }
    public int? https_score { get; set; }
    public string https_rank { get; set; }
    public int obs_score { get; set; }
    public string obs_rank { get; set; }
    public string users { get; set; }
    public string statuses { get; set; }
    public string connections { get; set; }
    public bool open_registrations { get; set; }
    public Info info { get; set; }
    public string thumbnail { get; set; }
    public string thumbnail_proxy { get; set; }
    public int? active_users { get; set; }
    public string email { get; set; }
    public string admin { get; set; }
}

public class Info
{
    public string short_description { get; set; }
    public string full_description { get; set; }
    public string topic { get; set; }
    public object[] languages { get; set; }
    public bool other_languages_accepted { get; set; }
    public string federates_with { get; set; }
    public object[] prohibited_content { get; set; }
    public object[] categories { get; set; }
}
