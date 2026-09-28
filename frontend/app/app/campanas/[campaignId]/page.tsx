"use client";

import { useParams } from "next/navigation";
import { AdvertisingDetailPanel } from "@/components/advertising-workspace";

export default function CampaignDetailPage() {
  const params = useParams<{ campaignId: string }>();
  return <AdvertisingDetailPanel kind="campaign" id={params.campaignId} />;
}
