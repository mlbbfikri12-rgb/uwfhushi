export type CustomerMe = {
  id: string;
  name: string;
  email: string;
  phone: string;
};

export type ClientAuthResponse = {
  token: string;
  customer: CustomerMe;
};

export type LoginPayload = {
  email: string;
  password: string;
};

export type RegisterPayload = {
  name: string;
  email: string;
  phone: string;
  password: string;
};

export type StaffAuthResponse = {
  token: string;
  staffId: string;
  role: "SUPER_ADMIN" | "SPV" | "FO";
  allowedBranchIds: string[];
  allowedBranches: {
    id: string;
    code: string;
    name: string;
  }[];
};
