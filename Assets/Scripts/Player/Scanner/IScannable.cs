

public interface IScannable
{
    void OnScanEnter(object scanner);
    void OnScanStay(object scanner, float dt);
    void OnScanExit(object scanner);    
}
