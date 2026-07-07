namespace AutoPartsStore.Web;

public static class CartConstants
{
    // Cookie, в котором храним анонимный идентификатор корзины для гостей
    // (пока пользователь не вошёл/не зарегистрировался).
    public const string GuestCookieName = "GuestCartId";
}
