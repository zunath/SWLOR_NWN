//a mettre dans le onOpen d'une porte pour qu'elle se referme automatiquement
// le delai peut etre ajuste

void main()
{
    DelayCommand(10.0, ActionCloseDoor(OBJECT_SELF));
}
