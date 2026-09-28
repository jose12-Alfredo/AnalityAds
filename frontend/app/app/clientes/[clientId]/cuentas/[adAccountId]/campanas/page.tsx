"use client";

import { useParams } from "next/navigation";
import { AccountHierarchyPanel } from "@/components/advertising-workspace";

export default function CampaignsPage() {
  const params = useParams<{ clientId: string; adAccountId: string }>();
  return <AccountHierarchyPanel clientId={params.clientId} adAccountId={params.adAccountId} />;
}
