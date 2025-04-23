export interface User{
    id: string;
    user: string;
    username: string;
    token: string;
    userRole: string;
    totpCode?: string;
}