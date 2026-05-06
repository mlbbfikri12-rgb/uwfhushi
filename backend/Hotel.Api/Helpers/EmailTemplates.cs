public static class EmailTemplates
{
    public static string Verification(string name, string link)
    {
        return $@"
        <div style='font-family:sans-serif'>
            <h2>Hi {name},</h2>
            <p>Terima kasih sudah mendaftar.</p>

            <p>
                <a href='{link}' 
                   style='padding:10px 20px;background:#1a1f3c;color:white;text-decoration:none'>
                   Verifikasi Email
                </a>
            </p>

            <p>Link berlaku 1 jam.</p>
        </div>";
    }
}