public interface ISwitcher
{
    void Select();
    void Deselect();
    void Disable();
    void EnableItem();
}

public interface ISwitcherWallHit
{
    void OnWallHit(bool Hit);
}

